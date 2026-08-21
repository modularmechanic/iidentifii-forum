using System.ComponentModel.DataAnnotations;
using Forum.Application.Common.Models;

namespace Forum.Application.Comments;

/// <summary>Which page of replies to return.</summary>
public sealed record CommentQuery
{
    [Range(1, PageBounds.MaxPage, ErrorMessage = "Page numbering starts at 1.")]
    public int Page { get; init; } = 1;

    [Range(1, PageBounds.MaxPageSize, ErrorMessage = "Page size must be between 1 and 100.")]
    public int PageSize { get; init; } = 20;
}
