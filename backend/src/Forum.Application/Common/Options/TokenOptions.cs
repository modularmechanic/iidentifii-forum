using System.ComponentModel.DataAnnotations;

namespace Forum.Application.Common.Options;

/// <summary>How one-time secrets are protected and how long each kind lasts.</summary>
public sealed class TokenOptions : IValidatableObject
{
    public const string SectionName = "Tokens";

    /// <summary>
    /// Mixed into every token hash. A leaked database is then not enough to forge a token, because
    /// the attacker also needs this value, which lives in configuration rather than in the data.
    /// </summary>
    [Required]
    [MinLength(32, ErrorMessage = "The token pepper must be at least 32 characters.")]
    public string Pepper { get; init; } = string.Empty;

    /// <summary>How long a verification or reset link stays usable.</summary>
    public TimeSpan LinkLifetime { get; init; } = TimeSpan.FromHours(1);

    /// <summary>How long an emailed sign-in code stays usable.</summary>
    public TimeSpan CodeLifetime { get; init; } = TimeSpan.FromMinutes(10);

    /// <summary>How long before the same kind of email may be sent to the same person again.</summary>
    public TimeSpan ResendCooldown { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// A lifetime that is not positive issues tokens nobody can use, and a cooldown that is not
    /// positive turns the resend guard off. Both are quiet failures, so they stop the boot instead.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (LinkLifetime <= TimeSpan.Zero)
        {
            yield return new ValidationResult(
                "The link lifetime must be longer than zero.",
                [nameof(LinkLifetime)]);
        }

        if (CodeLifetime <= TimeSpan.Zero)
        {
            yield return new ValidationResult(
                "The code lifetime must be longer than zero.",
                [nameof(CodeLifetime)]);
        }

        if (ResendCooldown <= TimeSpan.Zero)
        {
            yield return new ValidationResult(
                "The resend cooldown must be longer than zero.",
                [nameof(ResendCooldown)]);
        }
    }
}
