using System.ComponentModel.DataAnnotations;

namespace Forum.Application.Common.Options;

/// <summary>Facts about where the forum is reachable, used when building links for emails.</summary>
public sealed class AppOptions
{
    public const string SectionName = "App";

    /// <summary>The address a person's browser uses, which is not always where the API listens.</summary>
    [Required]
    [Url]
    public string PublicUrl { get; init; } = string.Empty;
}
