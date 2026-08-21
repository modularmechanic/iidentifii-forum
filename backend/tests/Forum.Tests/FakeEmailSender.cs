using System.Collections.Concurrent;
using Forum.Application.Common.Interfaces;

namespace Forum.Tests;

/// <summary>
/// Keeps the messages the forum tried to send, so a test can read a link or a code the way a
/// person would read their inbox.
/// </summary>
public sealed class FakeEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _sent = new();

    public IReadOnlyCollection<EmailMessage> Sent => _sent.ToArray();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        _sent.Enqueue(message);
        return Task.CompletedTask;
    }

    public EmailMessage? LastTo(string emailAddress)
        => _sent.LastOrDefault(message =>
            string.Equals(message.To, emailAddress, StringComparison.OrdinalIgnoreCase));

    /// <summary>Pulls the token out of a link, the way clicking it would.</summary>
    public string? LastTokenTo(string emailAddress) => TokenIn(LastTo(emailAddress)?.PlainTextBody);

    /// <summary>
    /// Every token sent to an address, so a test can check how many of the links that went out
    /// still work rather than only the newest.
    /// </summary>
    public IReadOnlyList<string> TokensTo(string emailAddress)
        => _sent
            .Where(message => string.Equals(message.To, emailAddress, StringComparison.OrdinalIgnoreCase))
            .Select(message => TokenIn(message.PlainTextBody))
            .OfType<string>()
            .ToList();

    public void Clear() => _sent.Clear();

    private static string? TokenIn(string? body)
    {
        if (body is null)
        {
            return null;
        }

        var match = System.Text.RegularExpressions.Regex.Match(body, @"token=([A-Za-z0-9_\-%]+)");
        return match.Success ? Uri.UnescapeDataString(match.Groups[1].Value) : null;
    }
}
