using Forum.Domain.Comments;
using Forum.Domain.Common;
using Forum.Domain.Users;

namespace Forum.Domain.Posts;

/// <summary>
/// A discussion someone started. Every rule about who may change it, who may like it and how
/// it is flagged lives here, so no caller can route around them.
/// </summary>
public sealed class Post
{
    public const int TitleMaxLength = 200;
    public const int BodyMaxLength = 10_000;

    private readonly List<PostLike> _likes = [];
    private readonly List<PostTag> _tags = [];
    private readonly List<Comment> _comments = [];

    private Post()
    {
    }

    public Guid Id { get; private set; }

    public Guid AuthorId { get; private set; }

    public User Author { get; private set; } = null!;

    public string Title { get; private set; } = null!;

    public string Body { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>When the author last edited it. Null while it is unchanged.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    public IReadOnlyCollection<PostLike> Likes => _likes;

    public IReadOnlyCollection<PostTag> Tags => _tags;

    public IReadOnlyCollection<Comment> Comments => _comments;

    public static Post Create(Guid authorId, string title, string body, DateTimeOffset now)
    {
        var cleanTitle = Clean(title, TitleMaxLength, "A title");
        var cleanBody = Clean(body, BodyMaxLength, "A body");

        return new Post
        {
            Id = Guid.CreateVersion7(),
            AuthorId = authorId,
            Title = cleanTitle,
            Body = cleanBody,
            CreatedAt = now,
        };
    }

    public void Update(Guid actorId, string title, string body, DateTimeOffset now)
    {
        EnsureOwnedBy(actorId);

        Title = Clean(title, TitleMaxLength, "A title");
        Body = Clean(body, BodyMaxLength, "A body");
        UpdatedAt = now;
    }

    /// <summary>Refuses anyone but the author. Deleting and editing both go through here.</summary>
    public void EnsureOwnedBy(Guid actorId)
    {
        if (AuthorId != actorId)
        {
            throw new DomainException("Only the author can change this discussion.", DomainError.Forbidden);
        }
    }

    /// <summary>Adds a reply, keeping the discussion and its replies consistent together.</summary>
    public Comment Reply(Guid authorId, string body, DateTimeOffset now)
    {
        var comment = Comment.Create(Id, authorId, body, now);
        _comments.Add(comment);
        return comment;
    }

    /// <summary>
    /// Records a like. Nobody likes their own discussion, and nobody likes the same one twice.
    /// </summary>
    public PostLike Like(Guid userId, DateTimeOffset now)
    {
        if (userId == AuthorId)
        {
            throw new DomainException("You cannot like your own discussion.", DomainError.RuleViolation);
        }

        if (_likes.Any(like => like.UserId == userId))
        {
            throw new DomainException("You have already liked this discussion.", DomainError.Conflict);
        }

        var like = PostLike.Create(Id, userId, now);
        _likes.Add(like);
        return like;
    }

    /// <summary>Removes a like. Returns the removed like, or null when there was none.</summary>
    public PostLike? Unlike(Guid userId)
    {
        var like = _likes.SingleOrDefault(candidate => candidate.UserId == userId);

        if (like is not null)
        {
            _likes.Remove(like);
        }

        return like;
    }

    /// <summary>
    /// Marks the discussion. Only a moderator may do this, and the rule lives here rather than
    /// only in the endpoint, so no other caller can route around it.
    /// </summary>
    public PostTag Flag(User moderator, ModerationTag tag, DateTimeOffset now)
    {
        if (moderator.Role != UserRole.Moderator)
        {
            throw new DomainException("Only a moderator can flag a discussion.", DomainError.Forbidden);
        }

        if (_tags.Any(existing => existing.Tag == tag))
        {
            throw new DomainException("This discussion already carries that flag.", DomainError.Conflict);
        }

        var postTag = PostTag.Create(Id, tag, moderator.Id, now);
        _tags.Add(postTag);
        return postTag;
    }

    /// <summary>
    /// Removes a flag. Returns the removed flag, or null when it was not present. Only a
    /// moderator may do this, and the rule lives here for the same reason it does on
    /// <see cref="Flag"/>: an endpoint attribute only guards the one caller that carries it.
    /// </summary>
    public PostTag? Unflag(User moderator, ModerationTag tag)
    {
        if (moderator.Role != UserRole.Moderator)
        {
            throw new DomainException(
                "Only a moderator can take a flag off a discussion.",
                DomainError.Forbidden);
        }

        var postTag = _tags.SingleOrDefault(existing => existing.Tag == tag);

        if (postTag is not null)
        {
            _tags.Remove(postTag);
        }

        return postTag;
    }

    private static string Clean(string value, int maxLength, string subject)
    {
        var trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            throw new DomainException($"{subject} is required.", DomainError.RuleViolation);
        }

        if (trimmed.Length > maxLength)
        {
            throw new DomainException(
                $"{subject} cannot be longer than {maxLength} characters.",
                DomainError.RuleViolation);
        }

        return trimmed;
    }
}
