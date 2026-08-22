using Forum.Application.Common.Interfaces;

namespace Forum.Infrastructure.Email;

/// <summary>
/// Actually puts a message somewhere: a mail server, or the log. Separate from
/// <see cref="IEmailSender"/> so that what the application calls and what finally delivers can be
/// different things, and delivery can happen after the request has been answered.
/// </summary>
public interface IEmailTransport
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
