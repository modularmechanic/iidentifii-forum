using Forum.Domain.Common;

namespace Forum.Domain.Users;

/// <summary>Someone with an account on the forum.</summary>
public sealed class User
{
    public const int UsernameMinLength = 3;
    public const int UsernameMaxLength = 32;
    public const int EmailMaxLength = 254;

    private User()
    {
    }

    public Guid Id { get; private set; }

    public string Username { get; private set; } = null!;

    public string Email { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public UserRole Role { get; private set; }

    /// <summary>When the address was confirmed. Null until then, which blocks signing in.</summary>
    public DateTimeOffset? EmailVerifiedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsEmailVerified => EmailVerifiedAt is not null;

    public static User Register(
        string username,
        string email,
        string passwordHash,
        DateTimeOffset now,
        UserRole role = UserRole.Member)
    {
        var trimmedUsername = username?.Trim() ?? string.Empty;
        var trimmedEmail = email?.Trim() ?? string.Empty;

        if (trimmedUsername.Length is < UsernameMinLength or > UsernameMaxLength)
        {
            throw new DomainException(
                $"A username must be between {UsernameMinLength} and {UsernameMaxLength} characters.",
                DomainError.RuleViolation);
        }

        if (trimmedEmail.Length == 0)
        {
            throw new DomainException("An email address is required.", DomainError.RuleViolation);
        }

        if (trimmedEmail.Length > EmailMaxLength)
        {
            throw new DomainException(
                $"An email address cannot be longer than {EmailMaxLength} characters.",
                DomainError.RuleViolation);
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("A password is required.", DomainError.RuleViolation);
        }

        return new User
        {
            Id = Guid.CreateVersion7(),
            Username = trimmedUsername,
            Email = trimmedEmail,
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = now,
        };
    }

    /// <summary>Confirms the address. Verifying twice is harmless and keeps the first timestamp.</summary>
    public void VerifyEmail(DateTimeOffset now) => EmailVerifiedAt ??= now;

    public void ChangePassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("A password is required.", DomainError.RuleViolation);
        }

        PasswordHash = passwordHash;
    }
}
