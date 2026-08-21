namespace Forum.Application.Dtos;

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
