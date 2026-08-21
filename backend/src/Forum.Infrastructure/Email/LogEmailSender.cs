using Forum.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Forum.Infrastructure.Email;

/// <summary>
/// Writes messages to the log instead of sending them, so the forum runs with no mail server at
/// all. The body is written out in full: without it, a verification link would be unreachable.
/// </summary>
public sealed class LogEmailSender(ILogger<LogEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Email to {Recipient}\nSubject: {Subject}\n{Body}",
            message.To,
            message.Subject,
            message.PlainTextBody);

        return Task.CompletedTask;
    }
}
