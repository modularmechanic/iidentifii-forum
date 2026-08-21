using Forum.Domain.Users;

namespace Forum.Application.Common.Interfaces;

/// <summary>A signed session token and when it stops being accepted.</summary>
public sealed record SessionToken(string Value, DateTimeOffset ExpiresAt);

/// <summary>Issues the token a signed-in member presents on later requests.</summary>
public interface ITokenService
{
    SessionToken Issue(User user);
}
