using Forum.Domain.Posts;
using System.ComponentModel.DataAnnotations;

namespace Forum.Application.Dtos;

/// <summary>Which mark a moderator is applying.</summary>
public sealed record FlagPostRequest
{
    [Required]
    [EnumDataType(typeof(ModerationTag))]
    public ModerationTag Tag { get; init; } = ModerationTag.MisleadingOrFalse;
}
