using Forum.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Forum.Infrastructure.Email;

/// <summary>Hands a message to an SMTP server. Locally that is Mailpit, which keeps it for reading.</summary>
public sealed class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    : IEmailTransport
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var mail = new MimeMessage
        {
            Subject = message.Subject,
            Body = new TextPart("plain") { Text = message.PlainTextBody },
        };

        mail.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        mail.To.Add(MailboxAddress.Parse(message.To));

        using var client = new SmtpClient();

        var security = _options.UseStartTls
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.None;

        await client.ConnectAsync(_options.Host, _options.Port, security, cancellationToken);

        if (!string.IsNullOrEmpty(_options.Username))
        {
            await client.AuthenticateAsync(
                _options.Username,
                _options.Password ?? string.Empty,
                cancellationToken);
        }

        await client.SendAsync(mail, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);

        logger.LogInformation("Sent \"{Subject}\" to {Recipient}.", message.Subject, message.To);
    }
}
