using System.Security.Claims;
using System.Text;
using Forum.Application.Common.Interfaces;
using Forum.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Forum.Infrastructure.Auth;

/// <summary>Signs the token a member presents after signing in.</summary>
public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider clock) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    public SessionToken Issue(User user)
    {
        var expiresAt = clock.GetUtcNow().Add(_options.Lifetime);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = expiresAt.UtcDateTime,
            IssuedAt = clock.GetUtcNow().UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [ClaimNames.Subject] = user.Id.ToString(),
                [ClaimNames.Username] = user.Username,
                [ClaimNames.Role] = user.Role.ToString(),
            },
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
        };

        return new SessionToken(new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }
}
