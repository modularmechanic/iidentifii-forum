using System.ComponentModel.DataAnnotations;
using Forum.Application.Common.Models;

namespace Forum.Application.Posts;

/// <summary>Which page of discussions to return.</summary>
public sealed record PostQuery
{
    [Range(1, PageBounds.MaxPage, ErrorMessage = "Page numbering starts at 1.")]
    public int Page { get; init; } = 1;

    [Range(1, PageBounds.MaxPageSize, ErrorMessage = "Page size must be between 1 and 100.")]
    public int PageSize { get; init; } = 20;
}
