using System.Threading.Channels;
using Forum.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Forum.Infrastructure.Email;

/// <summary>
/// Accepts a message and returns, leaving delivery to a background worker.
///
/// This exists for a timing reason rather than a throughput one. The endpoints that must not
/// reveal whether an address is registered answer 202 either way, but an address that *is*
/// registered used to wait for the mail server while an address that is not returned at once.
/// That difference is measurable, and measuring it is enough to tell the two apart — which is the
/// one thing those endpoints promise not to allow.
/// </summary>
public sealed class QueuedEmailSender(EmailQueue queue) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        => queue.EnqueueAsync(message, cancellationToken).AsTask();
}

/// <summary>The messages waiting to be delivered.</summary>
public sealed class EmailQueue
{
    // Bounded, so a mail server that has stopped answering cannot be absorbed indefinitely; the
    // oldest waiting message is dropped rather than letting the queue grow without limit.
    private readonly Channel<EmailMessage> _messages = Channel.CreateBounded<EmailMessage>(
        new BoundedChannelOptions(capacity: 1_000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
        });

    public ValueTask EnqueueAsync(EmailMessage message, CancellationToken cancellationToken)
        => _messages.Writer.WriteAsync(message, cancellationToken);

    public IAsyncEnumerable<EmailMessage> ReadAllAsync(CancellationToken cancellationToken)
        => _messages.Reader.ReadAllAsync(cancellationToken);
}

/// <summary>
/// Delivers what the queue holds. A failure is logged and the next message is attempted: the
/// caller has already been answered, so there is nobody left to tell.
/// </summary>
public sealed class EmailDispatcher(
    EmailQueue queue,
    IServiceScopeFactory scopes,
    ILogger<EmailDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var transport = scope.ServiceProvider.GetRequiredService<IEmailTransport>();

                await transport.SendAsync(message, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Could not deliver a message to {Recipient}. Subject: {Subject}",
                    message.To,
                    message.Subject);
            }
        }
    }
}
