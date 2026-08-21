using System.Security.Claims;
using Forum.Infrastructure.Auth;

namespace Forum.Api.Auth;

/// <summary>
/// Reads who is signed in. Controllers use this and pass an identifier into the services, so the
/// services never learn what an HTTP request is.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>The signed-in member, or null when the caller is anonymous.</summary>
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var subject = principal.FindFirstValue(ClaimNames.Subject);

        return Guid.TryParse(subject, out var id) ? id : null;
    }

    /// <summary>The signed-in member, where the endpoint has already required one.</summary>
    public static Guid GetRequiredUserId(this ClaimsPrincipal principal)
        => principal.GetUserId()
            ?? throw new InvalidOperationException("This endpoint requires an authenticated caller.");
}
