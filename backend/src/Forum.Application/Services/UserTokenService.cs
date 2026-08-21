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

    /// <summary>How long a link issued here stays usable, so a message can say so truthfully.</summary>
    public TimeSpan LinkLifetime => _options.LinkLifetime;

    /// <summary>
    /// Issues a token, retiring any earlier one for the same purpose so only the newest works.
    /// Returns the secret to send; only its hash is kept. Two requests arriving together cannot
    /// both leave a usable token behind: the database refuses the second, as a conflict.
    /// </summary>
    public async Task<string> IssueAsync(
        Guid userId,
        TokenPurpose purpose,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();

        // Retired by a direct update rather than through the change tracker, so the earlier token
        // is gone before the replacement is written and the two never briefly coexist.
        await database.UserTokens
            .Where(token => token.UserId == userId && token.Purpose == purpose && token.ConsumedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.ConsumedAt, (DateTimeOffset?)now),
                cancellationToken);

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

        return await TrySpendAsync(token.Id, now, cancellationToken) ? token : null;
    }

    /// <summary>
    /// Finds who a link belongs to, since a link carries no username, and spends it in the same
    /// step. Two requests carrying the same link cannot both be handed the token back.
    /// </summary>
    public async Task<UserToken?> ConsumeBySecretAsync(
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

        if (token is null || !token.IsUsable(now))
        {
            return null;
        }

        return await TrySpendAsync(token.Id, now, cancellationToken) ? token : null;
    }

    /// <summary>Spends every outstanding token for someone, whatever it was for.</summary>
    public async Task RetireAllAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();

        await database.UserTokens
            .Where(token => token.UserId == userId && token.ConsumedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.ConsumedAt, (DateTimeOffset?)now),
                cancellationToken);
    }

    /// <summary>
    /// Spends the token only while it is still unspent, in one statement. The database decides
    /// which of two requests arriving together wins; the loser is told the token is gone.
    /// </summary>
    private async Task<bool> TrySpendAsync(
        Guid tokenId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var spent = await database.UserTokens
            .Where(token => token.Id == tokenId && token.ConsumedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.ConsumedAt, (DateTimeOffset?)now),
                cancellationToken);

        return spent == 1;
    }
}
