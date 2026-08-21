using System.ComponentModel.DataAnnotations;

namespace Forum.Application.Posts;

/// <summary>Which page of discussions to return.</summary>
public sealed record PostQuery
{
    public const int MaxPageSize = 100;

    [Range(1, int.MaxValue, ErrorMessage = "Page numbering starts at 1.")]
    public int Page { get; init; } = 1;

    [Range(1, MaxPageSize, ErrorMessage = "Page size must be between 1 and 100.")]
    public int PageSize { get; init; } = 20;
}
