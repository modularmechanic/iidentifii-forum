using System.Collections.Concurrent;
using FluentAssertions;
using Forum.Application.Common.Interfaces;
using Forum.Infrastructure.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Forum.Tests.Infrastructure;

public sealed class EmailQueueTests
{
    [Fact]
    public async Task Accepting_a_message_does_not_wait_for_it_to_be_delivered()
    {
        var transport = new SlowTransport(TimeSpan.FromMilliseconds(400));
        var queue = new EmailQueue();
        var sender = new QueuedEmailSender(queue);

        var started = DateTimeOffset.UtcNow;
        await sender.SendAsync(new EmailMessage("someone@example.com", "Subject", "Body"));
        var accepted = DateTimeOffset.UtcNow - started;

        // The point of the queue: an address that exists must not take measurably longer to
        // answer than one that does not, and the mail server is what makes the difference.
        accepted.Should().BeLessThan(
            TimeSpan.FromMilliseconds(200),
            "accepting a message must not wait on the transport");

        transport.Sent.Should().BeEmpty("nothing has run the dispatcher yet");
    }

    [Fact]
    public async Task The_dispatcher_delivers_what_the_queue_holds()
    {
        var transport = new SlowTransport(TimeSpan.Zero);
        var queue = new EmailQueue();

        using var host = BuildHost(transport);
        var dispatcher = new EmailDispatcher(
            queue,
            host.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<EmailDispatcher>.Instance);

        await new QueuedEmailSender(queue).SendAsync(
            new EmailMessage("someone@example.com", "Confirm your email address", "A link."));

        using var stopping = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await dispatcher.StartAsync(stopping.Token);

        while (transport.Sent.IsEmpty && !stopping.Token.IsCancellationRequested)
        {
            await Task.Delay(20, CancellationToken.None);
        }

        await dispatcher.StopAsync(CancellationToken.None);

        transport.Sent.Should().ContainSingle()
            .Which.Subject.Should().Be("Confirm your email address");
    }

    [Fact]
    public async Task A_transport_that_throws_does_not_stop_the_ones_behind_it()
    {
        var transport = new SlowTransport(TimeSpan.Zero, failFirst: true);
        var queue = new EmailQueue();

        using var host = BuildHost(transport);
        var dispatcher = new EmailDispatcher(
            queue,
            host.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<EmailDispatcher>.Instance);

        var sender = new QueuedEmailSender(queue);
        await sender.SendAsync(new EmailMessage("first@example.com", "One", "Body"));
        await sender.SendAsync(new EmailMessage("second@example.com", "Two", "Body"));

        using var stopping = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await dispatcher.StartAsync(stopping.Token);

        while (transport.Sent.IsEmpty && !stopping.Token.IsCancellationRequested)
        {
            await Task.Delay(20, CancellationToken.None);
        }

        await dispatcher.StopAsync(CancellationToken.None);

        // The caller was answered long ago, so a failure has nobody to report to and must not
        // take the rest of the queue down with it.
        transport.Sent.Should().ContainSingle().Which.To.Should().Be("second@example.com");
    }

    private static ServiceProvider BuildHost(IEmailTransport transport)
    {
        var services = new ServiceCollection();
        services.AddSingleton(transport);

        return services.BuildServiceProvider();
    }

    private sealed class SlowTransport(TimeSpan delay, bool failFirst = false) : IEmailTransport
    {
        private int _attempts;

        public ConcurrentBag<EmailMessage> Sent { get; } = [];

        public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken);
            }

            if (failFirst && Interlocked.Increment(ref _attempts) == 1)
            {
                throw new InvalidOperationException("The mail server refused this one.");
            }

            Sent.Add(message);
        }
    }
}
