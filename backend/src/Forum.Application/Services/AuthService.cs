using Forum.Application.Common.Exceptions;
using Forum.Application.Common.Interfaces;
using Forum.Application.Common.Options;
using Forum.Application.Common.Security;
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
    ITokenService sessions,
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
        catch (ConflictException conflict) when (IsEmailTaken(conflict))
        {
            // Somebody registering an address they already hold has almost always forgotten the
            // account rather than found somebody else's, so the answer says what to do next.
            throw new ConflictException(
                "An account already exists for that email address. Sign in, or reset your "
                + "password if you have forgotten it.");
        }
        catch (ConflictException)
        {
            throw new ConflictException("That username is already taken.");
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

    /// <summary>
    /// Checks the password and, when it is right, emails a code. The password alone is never
    /// enough: the second step proves the person also reads the address on the account.
    /// </summary>
    public async Task<LoginChallengeResponse> BeginSignInAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await database.Users.SingleOrDefaultAsync(
            candidate => candidate.Username == request.Username,
            cancellationToken);

        // The hash is verified even when nobody matches, so a wrong username and a wrong password
        // take about the same time and cannot be told apart by how long the answer takes.
        var isPasswordCorrect = VerifyPassword(user, request.Password);

        if (user is null || !isPasswordCorrect)
        {
            throw new AuthenticationException("That username and password do not match.");
        }

        if (!user.IsEmailVerified)
        {
            throw new EmailNotVerifiedException(
                "Confirm your email address before signing in. We can send the link again.");
        }

        var challenge = await IssueSignInCodeAsync(user, cancellationToken);

        return new LoginChallengeResponse(challenge.Id, MaskEmail(user.Email), challenge.ExpiresAt);
    }

    /// <summary>
    /// Sends a code and returns the challenge it belongs to. Only one unspent token may exist per
    /// account, so two sign-ins arriving together race: the loser is refused by the database. It
    /// then answers with the challenge that won, because the code already on its way to the same
    /// inbox is the one that will work.
    /// </summary>
    private async Task<UserToken> IssueSignInCodeAsync(User user, CancellationToken cancellationToken)
    {
        try
        {
            var (challenge, code) = await tokens.IssueAsync(
                user.Id,
                TokenPurpose.TwoFactor,
                cancellationToken);

            await email.SendAsync(
                AuthEmails.SignInCode(user.Email, user.Username, code, tokens.CodeLifetime),
                cancellationToken);

            return challenge;
        }
        catch (ConflictException)
        {
            logger.LogInformation("Sign-in code already being sent for this account; reusing it.");

            return await tokens.FindOutstandingAsync(user.Id, TokenPurpose.TwoFactor, cancellationToken)
                ?? throw new AuthenticationException("That username and password do not match.");
        }
    }

    /// <summary>Exchanges a correct code for a session.</summary>
    public async Task<AuthenticatedResponse> CompleteSignInAsync(
        VerifyTwoFactorRequest request,
        CancellationToken cancellationToken)
    {
        var redeemed = await tokens.RedeemAsync(
            request.ChallengeId,
            TokenPurpose.TwoFactor,
            request.Code,
            cancellationToken)
            ?? throw new AuthenticationException("That code is wrong or has expired.");

        var user = await database.Users.SingleAsync(
            candidate => candidate.Id == redeemed.UserId,
            cancellationToken);

        var session = sessions.Issue(user);

        return new AuthenticatedResponse(
            session.Value,
            session.ExpiresAt,
            new UserDto(user.Id, user.Username, user.Email, user.Role));
    }

    /// <summary>
    /// Sends a link to set a new password. Always reports the same thing, so the body of the
    /// reply says nothing about who is registered. Nor does the time it takes: the send is
    /// accepted by a queue and carried out after the response, so neither answer waits for the
    /// mail server.
    /// </summary>
    public async Task ForgotPasswordAsync(string emailAddress, CancellationToken cancellationToken)
    {
        var user = await database.Users.SingleOrDefaultAsync(
            candidate => candidate.Email == emailAddress,
            cancellationToken);

        if (user is null)
        {
            logger.LogInformation("Password reset requested for an address with no account.");
            return;
        }

        if (await tokens.IsWithinCooldownAsync(user.Id, TokenPurpose.PasswordReset, cancellationToken))
        {
            logger.LogInformation("Password reset refused: still within the cooldown.");
            return;
        }

        // Two requests can read the cooldown at the same moment and both find it passed. The
        // database refuses the second link, and the one already on its way stands.
        //
        // This must be swallowed rather than surfaced: a conflict can only arise for an address
        // that has an account, so letting it out would answer 409 here and 202 for an address
        // nobody registered — telling a caller exactly what this endpoint promises not to.
        try
        {
            var (_, token) = await tokens.IssueAsync(
                user.Id,
                TokenPurpose.PasswordReset,
                cancellationToken);

            var link = $"{app.Value.PublicUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}";

            await email.SendAsync(
                AuthEmails.PasswordReset(user.Email, user.Username, link, tokens.LinkLifetime),
                cancellationToken);
        }
        catch (ConflictException)
        {
            logger.LogInformation("Password reset refused: another request is already sending one.");
        }
    }

    /// <summary>
    /// Sets a new password and retires every outstanding token, so a reset link that somebody else
    /// also holds stops working the moment the password changes.
    /// </summary>
    public async Task ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var token = await tokens.ConsumeBySecretAsync(
            TokenPurpose.PasswordReset,
            request.Token,
            cancellationToken)
            ?? throw new InvalidTokenException("This link has expired or has already been used.");

        var user = await database.Users.SingleAsync(
            candidate => candidate.Id == token.UserId,
            cancellationToken);

        user.ChangePassword(passwordHasher.HashPassword(user, request.NewPassword));

        // Somebody resetting a password may be recovering from a compromise, so nothing issued
        // before now should still work.
        await tokens.RetireAllAsync(user.Id, cancellationToken);

        await database.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Describes the signed-in member to themselves.</summary>
    public async Task<UserDto> GetSignedInAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await database.Users.SingleOrDefaultAsync(
            candidate => candidate.Id == userId,
            cancellationToken)
            ?? throw new NotFoundException("That account no longer exists.");

        return new UserDto(user.Id, user.Username, user.Email, user.Role);
    }

    private bool VerifyPassword(User? user, string password)
    {
        // A throwaway hash keeps the work the same when nobody matches the username.
        var target = user ?? User.Register("placeholder", "placeholder@forum.local", "hash", clock.GetUtcNow());
        var hash = user?.PasswordHash ?? EmptyPasswordHash.Value;

        return passwordHasher.VerifyHashedPassword(target, hash, password) != PasswordVerificationResult.Failed;
    }

    /// <summary>Shows enough of an address to recognise it, and no more.</summary>
    private static string MaskEmail(string emailAddress)
    {
        var at = emailAddress.IndexOf('@', StringComparison.Ordinal);
        if (at <= 1)
        {
            return emailAddress;
        }

        return $"{emailAddress[0]}{new string('*', Math.Min(at - 1, 4))}{emailAddress[at..]}";
    }

    /// <summary>Which of the two unique columns refused the write, so each can say its own piece.</summary>
    private static bool IsEmailTaken(ConflictException conflict)
        => conflict.ConstraintName?.Contains("Email", StringComparison.OrdinalIgnoreCase) == true;

    private async Task SendVerificationAsync(User user, CancellationToken cancellationToken)
    {
        var (_, token) = await tokens.IssueAsync(user.Id, TokenPurpose.EmailVerification, cancellationToken);
        var link = $"{app.Value.PublicUrl.TrimEnd('/')}/verify-email?token={Uri.EscapeDataString(token)}";

        var message = AuthEmails.Verification(
            user.Email,
            user.Username,
            link,
            tokens.LinkLifetime);

        await email.SendAsync(message, cancellationToken);
    }
}
