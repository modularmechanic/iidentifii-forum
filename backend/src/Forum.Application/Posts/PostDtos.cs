using Forum.Domain.Posts;

namespace Forum.Application.Posts;

/// <summary>Who wrote something, as shown next to it.</summary>
public sealed record AuthorDto(Guid Id, string Username);

/// <summary>A moderation flag as the reader sees it.</summary>
public sealed record ModerationTagDto(ModerationTag Tag, string TaggedByUsername, DateTimeOffset CreatedAt);

/// <summary>A discussion, in the shape both the list and the detail view need.</summary>
public sealed record PostDto(
    Guid Id,
    string Title,
    string Body,
    AuthorDto Author,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    int LikeCount,
    int CommentCount,
    IReadOnlyList<ModerationTagDto> Tags,
    bool LikedByMe);
