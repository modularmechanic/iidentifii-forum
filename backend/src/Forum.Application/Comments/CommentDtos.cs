using Forum.Application.Posts;

namespace Forum.Application.Comments;

/// <summary>A reply as the reader sees it.</summary>
public sealed record CommentDto(
    Guid Id,
    Guid PostId,
    string Body,
    AuthorDto Author,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
