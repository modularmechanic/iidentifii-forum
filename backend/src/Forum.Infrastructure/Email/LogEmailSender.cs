using Forum.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Forum.Infrastructure.Email;

/// <summary>
/// Drops messages instead of sending them, so the forum runs with no mail server at all. Nothing
/// about the message reaches the log: the body carries working links and sign-in codes, and the
/// recipient is somebody's address. Run Mailpit when the messages themselves need reading.
/// </summary>
public sealed class LogEmailSender(ILogger<LogEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Email delivery is disabled; a message was dropped instead of sent.");

        return Task.CompletedTask;
    }
}
