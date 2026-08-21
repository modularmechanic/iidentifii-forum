using FluentAssertions;
using Forum.Application.Common.Interfaces;
using Forum.Infrastructure.Email;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Forum.Tests.Infrastructure;

/// <summary>
/// Turning email off writes the message to the log instead of sending it. The body carries
/// confirmation links, reset links and sign-in codes, so where it is allowed to go matters.
/// </summary>
public sealed class LogEmailSenderTests
{
    private static readonly EmailMessage Message = new(
        To: "reader@example.com",
        Subject: "Your sign-in code",
        PlainTextBody: "Your sign-in code is 123456");

    [Fact]
    public async Task Development_writes_the_body_out_so_the_link_can_be_followed()
    {
        var log = new RecordingLogger();
        var sender = new LogEmailSender(new StubEnvironment("Development"), log);

        await sender.SendAsync(Message);

        log.Entries.Should().ContainSingle()
            .Which.Should().Contain("Your sign-in code is 123456");
    }

    /// <summary>
    /// Silencing a broken mail server should not turn every account recovery secret into log
    /// output, which log shipping would then copy somewhere else again.
    /// </summary>
    [Fact]
    public async Task Anywhere_else_keeps_the_body_out_of_the_log()
    {
        var log = new RecordingLogger();
        var sender = new LogEmailSender(new StubEnvironment("Production"), log);

        await sender.SendAsync(Message);

        var entry = log.Entries.Should().ContainSingle().Subject;
        entry.Should().NotContain("123456");
        entry.Should().Contain("reader@example.com", "an operator still needs to see nothing was sent");
    }

    private sealed class StubEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Forum.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class RecordingLogger : ILogger<LogEmailSender>
    {
        private readonly List<string> _entries = [];

        public IReadOnlyList<string> Entries => _entries;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => _entries.Add(formatter(state, exception));
    }
}
