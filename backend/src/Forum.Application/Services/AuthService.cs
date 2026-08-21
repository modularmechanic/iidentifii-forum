using Forum.Application.Common.Exceptions;
using Forum.Application.Common.Interfaces;
using Forum.Application.Common.Options;
using Forum.Application.Dtos;
using Forum.Application.Emails;
using Forum.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Forum.Application.Services;

/// <summary>Creating an account and proving the address on it belongs to you.</summary>
public sealed class AuthService(
    IForumDbContext database,
    UserTokenService tokens,
    IPasswordHasher<User> passwordHasher,
    IEmailSender email,
    TimeProvider clock,
    IOptions<AppOptions> app,
    ILogger<AuthService> logger)
{
    /// <summary>
    /// Creates an account and sends a verification link. The account cannot sign in until the
    /// address is confirmed, so a mistyped address costs nothing but a resend.
    /// </summary>
    public async Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var user = User.Register(
            request.Username,
            request.Email,
            passwordHash: "placeholder",
            clock.GetUtcNow());

        // Hashing needs the user the hash belongs to, so the real value is set once it exists.
        user.ChangePassword(passwordHasher.HashPassword(user, request.Password));

        database.Users.Add(user);

        // Uniqueness is decided by the database rather than by reading first, which would still
        // let two registrations of the same name pass at the same moment.
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (ConflictException)
        {
            throw new ConflictException("That username or email address is already registered.");
        }

        await SendVerificationAsync(user, cancellationToken);
    }

    /// <summary>Confirms an address. The link works once.</summary>
    public async Task VerifyEmailAsync(string token, CancellationToken cancellationToken)
    {
        // Found and spent in one step, so two requests carrying the same link cannot both confirm.
        var userToken = await tokens.ConsumeBySecretAsync(
            TokenPurpose.EmailVerification,
            token,
            cancellationToken);

        if (userToken is null)
        {
            throw new InvalidTokenException("This link has expired or has already been used.");
        }

        var user = await database.Users.SingleAsync(
            candidate => candidate.Id == userToken.UserId,
            cancellationToken);

        user.VerifyEmail(clock.GetUtcNow());

        await database.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Sends another verification link. Always reports the same thing, whether or not the address
    /// belongs to an account, so the reply cannot be used to find out who is registered.
    /// </summary>
    public async Task ResendVerificationAsync(string emailAddress, CancellationToken cancellationToken)
    {
        var user = await database.Users.SingleOrDefaultAsync(
            candidate => candidate.Email == emailAddress,
            cancellationToken);

        if (user is null || user.IsEmailVerified)
        {
            logger.LogInformation("Verification resend requested for an address that cannot use one.");
            return;
        }

        if (await tokens.IsWithinCooldownAsync(user.Id, TokenPurpose.EmailVerification, cancellationToken))
        {
            logger.LogInformation("Verification resend refused: still within the cooldown.");
            return;
        }

        // Two requests can read the cooldown at the same moment and both find it passed. The
        // database then refuses the second replacement link, and the one already on its way stands.
        try
        {
            await SendVerificationAsync(user, cancellationToken);
        }
        catch (ConflictException)
        {
            logger.LogInformation("Verification resend refused: another request is already sending one.");
        }
    }

    private async Task SendVerificationAsync(User user, CancellationToken cancellationToken)
    {
        var token = await tokens.IssueAsync(user.Id, TokenPurpose.EmailVerification, cancellationToken);
        var link = $"{app.Value.PublicUrl.TrimEnd('/')}/verify-email?token={Uri.EscapeDataString(token)}";

        var message = AuthEmails.Verification(
            user.Email,
            user.Username,
            link,
            tokens.LinkLifetime);

        await email.SendAsync(message, cancellationToken);
    }
}
