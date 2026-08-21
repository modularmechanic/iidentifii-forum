using Forum.Domain.Posts;

namespace Forum.Application.Dtos;

/// <summary>A moderation flag as the reader sees it.</summary>
public sealed record ModerationTagDto(ModerationTag Tag, string TaggedByUsername, DateTimeOffset CreatedAt);
