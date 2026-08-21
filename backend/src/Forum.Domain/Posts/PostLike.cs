namespace Forum.Domain.Posts;

/// <summary>
/// One member's like of one discussion. The pair is the key, so the database itself
/// enforces that nobody likes the same discussion twice.
/// </summary>
public sealed class PostLike
{
    private PostLike()
    {
    }

    public Guid PostId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    internal static PostLike Create(Guid postId, Guid userId, DateTimeOffset now)
        => new() { PostId = postId, UserId = userId, CreatedAt = now };
}
