using System.ComponentModel.DataAnnotations;

namespace Forum.Infrastructure.Email;

/// <summary>Where outgoing mail is handed over, and who it comes from.</summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    [Required]
    public string Host { get; init; } = "localhost";

    [Range(1, 65535)]
    public int Port { get; init; } = 1025;

    [Required]
    [EmailAddress]
    public string FromAddress { get; init; } = "no-reply@forum.local";

    [Required]
    public string FromName { get; init; } = "iiDENTIFii Forum";

    /// <summary>Left empty for a local mail catcher, which wants no credentials.</summary>
    public string? Username { get; init; }

    public string? Password { get; init; }

    public bool UseStartTls { get; init; }
}
