using System.ComponentModel.DataAnnotations;

namespace Forum.Infrastructure.Auth;

/// <summary>How session tokens are signed and how long they last.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// The signing key. Anyone holding it can mint a session for any member, so it belongs in a
    /// secret store. Its length is checked at startup, because a short key weakens the signature
    /// quietly rather than failing.
    /// </summary>
    [Required]
    [MinLength(32, ErrorMessage = "The signing key must be at least 32 characters.")]
    public string SigningKey { get; init; } = string.Empty;

    [Required]
    public string Issuer { get; init; } = "iidentifii-forum";

    [Required]
    public string Audience { get; init; } = "iidentifii-forum";

    public TimeSpan Lifetime { get; init; } = TimeSpan.FromHours(8);
}
