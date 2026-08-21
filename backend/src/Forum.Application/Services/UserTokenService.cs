using Forum.Application.Common.Interfaces;
using Forum.Application.Common.Options;
using Forum.Application.Common.Security;
using Forum.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Forum.Application.Services;

/// <summary>
/// Issues, checks and spends the one-time secrets sent by email. Every flow that mails somebody a
/// token goes through here, so expiry, attempt limits and the resend cooldown behave the same way
/// whichever flow it is.
/// </summary>
public sealed class UserTokenService(
    IForumDbContext database,
    SecretHasher hasher,
    TimeProvider clock,
    IOptions<TokenOptions> options)
{
    private readonly TokenOptions _options = options.Value;

    /// <summary>
    /// Issues a token, retiring any earlier one for the same purpose so only the newest works.
    /// Returns the secret to send; only its hash is kept.
    /// </summary>
    public async Task<string> IssueAsync(
        Guid userId,
        TokenPurpose purpose,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();

        await RetireOutstandingAsync(userId, purpose, now, cancellationToken);

        var secret = purpose == TokenPurpose.TwoFactor
            ? SecretGenerator.NewCode()
            : SecretGenerator.NewLinkToken();

        var lifetime = purpose == TokenPurpose.TwoFactor
            ? _options.CodeLifetime
            : _options.LinkLifetime;

        database.UserTokens.Add(UserToken.Issue(userId, purpose, hasher.Hash(secret), lifetime, now));
        await database.SaveChangesAsync(cancellationToken);

        return secret;
    }

    /// <summary>
    /// True when the same kind of message was sent recently. Callers stay silent rather than
    /// telling the requester, so the answer cannot be used to learn whether an account exists.
    /// </summary>
    public async Task<bool> IsWithinCooldownAsync(
        Guid userId,
        TokenPurpose purpose,
        CancellationToken cancellationToken)
    {
        var sentAfter = clock.GetUtcNow() - _options.ResendCooldown;

        return await database.UserTokens.AnyAsync(
            token => token.UserId == userId && token.Purpose == purpose && token.CreatedAt > sentAfter,
            cancellationToken);
    }

    /// <summary>
    /// Spends the token when the secret matches. A wrong secret is recorded against the newest
    /// outstanding token, so guessing runs out of attempts rather than continuing indefinitely.
    /// </summary>
    public async Task<UserToken?> RedeemAsync(
        Guid userId,
        TokenPurpose purpose,
        string secret,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();

        var token = await database.UserTokens
            .Where(candidate => candidate.UserId == userId && candidate.Purpose == purpose)
            .OrderByDescending(candidate => candidate.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (token is null || !token.IsUsable(now))
        {
            return null;
        }

        if (!hasher.Matches(secret, token.SecretHash))
        {
            token.RecordFailedAttempt(now);
            await database.SaveChangesAsync(cancellationToken);
            return null;
        }

        token.Consume(now);
        await database.SaveChangesAsync(cancellationToken);
        return token;
    }

    /// <summary>Finds who a link belongs to, since a link carries no username.</summary>
    public async Task<UserToken?> FindUsableBySecretAsync(
        TokenPurpose purpose,
        string secret,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var secretHash = hasher.Hash(secret);

        // Looked up by hash, so the secret itself is never compared in the database.
        var token = await database.UserTokens
            .SingleOrDefaultAsync(
                candidate => candidate.Purpose == purpose && candidate.SecretHash == secretHash,
                cancellationToken);

        return token?.IsUsable(now) == true ? token : null;
    }

    /// <summary>Spends every outstanding token for someone, whatever it was for.</summary>
    public async Task RetireAllAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();

        var tokens = await database.UserTokens
            .Where(token => token.UserId == userId && token.ConsumedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Consume(now);
        }

        await database.SaveChangesAsync(cancellationToken);
    }

    private async Task RetireOutstandingAsync(
        Guid userId,
        TokenPurpose purpose,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var outstanding = await database.UserTokens
            .Where(token => token.UserId == userId && token.Purpose == purpose && token.ConsumedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in outstanding)
        {
            token.Consume(now);
        }
    }
}
