using System.Security.Claims;

namespace Forum.Infrastructure.Auth;

/// <summary>
/// The claims a session token carries. Named here so the side that writes them and the side that
/// reads them cannot disagree.
/// </summary>
public static class ClaimNames
{
    public const string Subject = "sub";
    public const string Username = "name";
    public const string Role = ClaimTypes.Role;
}
