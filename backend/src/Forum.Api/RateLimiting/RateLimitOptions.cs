using System.ComponentModel.DataAnnotations;

namespace Forum.Api.RateLimiting;

/// <summary>
/// How many requests one caller may make a minute. Configurable so a deployment can be tightened
/// or loosened without a rebuild, and so tests can exercise the limit deliberately.
/// </summary>
public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    [Range(1, int.MaxValue)]
    public int GlobalPermitsPerMinute { get; init; } = 120;

    /// <summary>Applied to sign-in, registration and anything else that sends email.</summary>
    [Range(1, int.MaxValue)]
    public int AuthenticationPermitsPerMinute { get; init; } = 10;
}
