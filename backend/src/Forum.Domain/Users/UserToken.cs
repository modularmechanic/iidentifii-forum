using Forum.Domain.Common;

namespace Forum.Domain.Users;

/// <summary>
/// A one-time secret sent by email: a verification link, a sign-in code, or a reset link.
/// Only its hash is stored, so a leaked database does not hand out working tokens.
/// </summary>
public sealed class UserToken
{
    /// <summary>Wrong guesses allowed before the token is spent. Applies to sign-in codes.</summary>
    public const int MaxFailedAttempts = 5;

    private UserToken()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public TokenPurpose Purpose { get; private set; }

    public string SecretHash { get; private set; } = null!;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    public int FailedAttempts { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static UserToken Issue(
        Guid userId,
        TokenPurpose purpose,
        string secretHash,
        TimeSpan lifetime,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(secretHash))
        {
            throw new DomainException("A token secret is required.", DomainError.RuleViolation);
        }

        if (lifetime <= TimeSpan.Zero)
        {
            throw new DomainException("A token must outlive the moment it is issued.", DomainError.RuleViolation);
        }

        return new UserToken
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Purpose = purpose,
            SecretHash = secretHash,
            ExpiresAt = now.Add(lifetime),
            CreatedAt = now,
        };
    }

    /// <summary>True while the token is unspent, unexpired and has attempts left.</summary>
    public bool IsUsable(DateTimeOffset now)
        => ConsumedAt is null && now < ExpiresAt && FailedAttempts < MaxFailedAttempts;

    public void Consume(DateTimeOffset now) => ConsumedAt ??= now;

    /// <summary>Records a wrong guess, spending the token once too many have been made.</summary>
    public void RecordFailedAttempt(DateTimeOffset now)
    {
        FailedAttempts++;

        if (FailedAttempts >= MaxFailedAttempts)
        {
            Consume(now);
        }
    }
}
