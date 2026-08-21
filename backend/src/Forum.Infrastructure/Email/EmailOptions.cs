using System.ComponentModel.DataAnnotations;

namespace Forum.Infrastructure.Email;

/// <summary>Where outgoing mail is handed over, and who it comes from.</summary>
public sealed class EmailOptions : IValidatableObject
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

    /// <summary>
    /// Half a credential is worse than none: a username alone authenticates with an empty password,
    /// and a password alone is never sent at all. Neither is visible from the outside, so the boot
    /// stops rather than letting mail fail later for a reason nobody can see.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(Username) != string.IsNullOrEmpty(Password))
        {
            yield return new ValidationResult(
                "An SMTP username and password must be given together, or neither given.",
                [nameof(Username), nameof(Password)]);
        }
    }
}
