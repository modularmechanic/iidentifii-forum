namespace Forum.Application.Common.Interfaces;

/// <summary>One message, as the forum wants it to arrive.</summary>
public sealed record EmailMessage(string To, string Subject, string PlainTextBody);

/// <summary>
/// Sends email. Implemented by a real sender and by one that writes to the log, so development
/// and tests never depend on a mail server being present.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
