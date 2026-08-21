using System.ComponentModel.DataAnnotations;

namespace Forum.Application.Comments;

/// <summary>Which page of replies to return.</summary>
public sealed record CommentQuery
{
    public const int MaxPageSize = 100;

    [Range(1, int.MaxValue, ErrorMessage = "Page numbering starts at 1.")]
    public int Page { get; init; } = 1;

    [Range(1, MaxPageSize, ErrorMessage = "Page size must be between 1 and 100.")]
    public int PageSize { get; init; } = 20;
}
