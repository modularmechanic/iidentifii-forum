using Forum.Api.Auth;
using Forum.Api.RateLimiting;
using Forum.Application.Dtos;
using Forum.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Forum.Api.Controllers;

/// <summary>
/// Creating an account and proving the address on it belongs to you. Every route here is limited
/// more tightly than the rest of the API, because each one either checks a secret or sends mail.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
[EnableRateLimiting(RateLimitPolicies.Authentication)]
public sealed class AuthController(AuthService auth) : ControllerBase
{
    /// <summary>Creates an account and sends a verification link.</summary>
    [HttpPost("register")]
    [ProducesResponseType<AcknowledgementResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AcknowledgementResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        await auth.RegisterAsync(request, cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new AcknowledgementResponse("Check your email to confirm your address."));
    }

    /// <summary>Confirms an address using the link that was emailed.</summary>
    [HttpPost("verify-email")]
    [ProducesResponseType<AcknowledgementResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AcknowledgementResponse>> VerifyEmail(
        VerifyEmailRequest request,
        CancellationToken cancellationToken)
    {
        await auth.VerifyEmailAsync(request.Token, cancellationToken);

        return Ok(new AcknowledgementResponse("Your email address is confirmed. You can sign in."));
    }

    /// <summary>
    /// Sends another verification link. Always accepted, whether or not the address belongs to an
    /// account, so the reply cannot be used to find out who is registered.
    /// </summary>
    [HttpPost("resend-verification")]
    [ProducesResponseType<AcknowledgementResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AcknowledgementResponse>> ResendVerification(
        ResendVerificationRequest request,
        CancellationToken cancellationToken)
    {
        await auth.ResendVerificationAsync(request.Email, cancellationToken);

        return Accepted(new AcknowledgementResponse(
            "If that address needs confirming, a new link is on its way."));
    }

    /// <summary>
    /// Checks the password and emails a code. The password alone does not sign anybody in.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType<LoginChallengeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LoginChallengeResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
        => Ok(await auth.BeginSignInAsync(request, cancellationToken));

    /// <summary>Exchanges the emailed code for a session.</summary>
    [HttpPost("verify-2fa")]
    [ProducesResponseType<AuthenticatedResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticatedResponse>> VerifyTwoFactor(
        VerifyTwoFactorRequest request,
        CancellationToken cancellationToken)
        => Ok(await auth.CompleteSignInAsync(request, cancellationToken));

    /// <summary>
    /// Sends a link to set a new password. Always accepted, so the reply cannot be used to find
    /// out who is registered.
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType<AcknowledgementResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AcknowledgementResponse>> ForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await auth.ForgotPasswordAsync(request.Email, cancellationToken);

        return Accepted(new AcknowledgementResponse(
            "If that address has an account, a link to set a new password is on its way."));
    }

    /// <summary>Sets a new password using the emailed link.</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType<AcknowledgementResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AcknowledgementResponse>> ResetPassword(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await auth.ResetPasswordAsync(request, cancellationToken);

        return Ok(new AcknowledgementResponse("Your password is changed. You can sign in with it."));
    }

    /// <summary>Who the caller is signed in as.</summary>
    [HttpGet("me")]
    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.Session)]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken)
        => Ok(await auth.GetSignedInAsync(User.GetRequiredUserId(), cancellationToken));
}
