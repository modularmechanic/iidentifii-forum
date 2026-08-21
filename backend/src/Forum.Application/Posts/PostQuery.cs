using System.ComponentModel.DataAnnotations;
using Forum.Application.Common.Models;
using Forum.Domain.Posts;

namespace Forum.Application.Posts;

/// <summary>
/// Which discussions to return, and in what order. Bounds are declared here so an unreasonable
/// request is refused before it reaches the database.
/// </summary>
public sealed record PostQuery : IValidatableObject
{
    [Range(1, PageBounds.MaxPage, ErrorMessage = "Page numbering starts at 1.")]
    public int Page { get; init; } = 1;

    [Range(1, PageBounds.MaxPageSize, ErrorMessage = "Page size must be between 1 and 100.")]
    public int PageSize { get; init; } = 20;

    /// <summary>Only discussions started on or after this day, in UTC.</summary>
    public DateOnly? From { get; init; }

    /// <summary>Only discussions started on or before this day, in UTC.</summary>
    public DateOnly? To { get; init; }

    /// <summary>Only discussions started by this member. Case does not matter.</summary>
    [StringLength(32, ErrorMessage = "A username cannot be longer than 32 characters.")]
    public string? Author { get; init; }

    /// <summary>Only discussions carrying this moderation flag.</summary>
    public ModerationTag? Tag { get; init; }

    public PostSort Sort { get; init; } = PostSort.CreatedAt;

    public SortOrder Order { get; init; } = SortOrder.Descending;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From is not null && To is not null && From > To)
        {
            yield return new ValidationResult(
                "The start of the range cannot be after its end.",
                [nameof(From), nameof(To)]);
        }
    }
}
