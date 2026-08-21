namespace Forum.Domain.Posts;

/// <summary>A moderation mark applied to a discussion, and who applied it.</summary>
public sealed class PostTag
{
    private PostTag()
    {
    }

    public Guid PostId { get; private set; }

    public ModerationTag Tag { get; private set; }

    public Guid TaggedByUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    internal static PostTag Create(Guid postId, ModerationTag tag, Guid moderatorId, DateTimeOffset now)
        => new() { PostId = postId, Tag = tag, TaggedByUserId = moderatorId, CreatedAt = now };
}
