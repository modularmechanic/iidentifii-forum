using Forum.Domain.Common;
using Forum.Domain.Users;

namespace Forum.Domain.Comments;

/// <summary>A reply to a discussion.</summary>
public sealed class Comment
{
    public const int BodyMaxLength = 2_000;

    private Comment()
    {
    }

    public Guid Id { get; private set; }

    public Guid PostId { get; private set; }

    public Guid AuthorId { get; private set; }

    public User Author { get; private set; } = null!;

    public string Body { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Comment Create(Guid postId, Guid authorId, string body, DateTimeOffset now)
        => new()
        {
            Id = Guid.CreateVersion7(),
            PostId = postId,
            AuthorId = authorId,
            Body = Clean(body),
            CreatedAt = now,
        };

    public void Update(Guid actorId, string body, DateTimeOffset now)
    {
        EnsureOwnedBy(actorId);

        Body = Clean(body);
        UpdatedAt = now;
    }

    public void EnsureOwnedBy(Guid actorId)
    {
        if (AuthorId != actorId)
        {
            throw new DomainException("Only the author can change this reply.", DomainError.Forbidden);
        }
    }

    private static string Clean(string body)
    {
        var trimmed = body?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            throw new DomainException("A reply is required.", DomainError.RuleViolation);
        }

        if (trimmed.Length > BodyMaxLength)
        {
            throw new DomainException(
                $"A reply cannot be longer than {BodyMaxLength} characters.",
                DomainError.RuleViolation);
        }

        return trimmed;
    }
}
