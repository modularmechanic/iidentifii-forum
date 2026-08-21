using Forum.Domain.Posts;
using System.ComponentModel.DataAnnotations;

namespace Forum.Application.Dtos;

/// <summary>What a member supplies to start a discussion.</summary>
public sealed record CreatePostRequest
{
    [Required]
    [StringLength(Post.TitleMaxLength, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    [Required]
    [StringLength(Post.BodyMaxLength, MinimumLength = 1)]
    public string Body { get; init; } = string.Empty;
}
