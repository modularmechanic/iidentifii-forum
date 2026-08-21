using Forum.Domain.Comments;
using System.ComponentModel.DataAnnotations;

namespace Forum.Application.Dtos;

/// <summary>What a member supplies to reply to a discussion.</summary>
public sealed record CreateCommentRequest
{
    [Required]
    [StringLength(Comment.BodyMaxLength, MinimumLength = 1)]
    public string Body { get; init; } = string.Empty;
}
