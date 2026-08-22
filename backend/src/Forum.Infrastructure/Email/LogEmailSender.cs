using Forum.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Forum.Infrastructure.Email;

/// <summary>
/// Writes messages to the log instead of sending them, so the forum runs with no mail server at
/// all.
/// </summary>
/// <remarks>
/// The body holds confirmation links, password reset links and sign-in codes, so it is written
/// out only in development, where reaching them is the whole point of running without a mail
/// server. Anywhere else the environment is checked the same way seeding is: turning email off to
/// quieten a broken SMTP host should not turn every account recovery secret into log output that
/// log shipping then copies somewhere else.
/// </remarks>
public sealed class LogEmailSender(
    IHostEnvironment environment,
    ILogger<LogEmailSender> logger) : IEmailTransport
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment())
        {
            logger.LogWarning(
                "Email is disabled, so nothing was sent to {Recipient}. Subject: {Subject}",
                message.To,
                message.Subject);

            return Task.CompletedTask;
        }

        logger.LogInformation(
            "Email to {Recipient}\nSubject: {Subject}\n{Body}",
            message.To,
            message.Subject,
            message.PlainTextBody);

        return Task.CompletedTask;
    }
}
