using System.Text;
using Forum.Domain.Users;
using Forum.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Forum.Api.Auth;

public static class AuthenticationSetup
{
    /// <summary>Moderators can flag content; everything else a member can do needs only a session.</summary>
    public const string ModeratorPolicy = "Moderator";

    public static IServiceCollection AddForumAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException(
                "The Jwt section is missing. See docs/reference/configuration.md.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(bearer =>
            {
                // Claims are read exactly as they were written, rather than renamed to the older
                // schema names, so the two sides cannot disagree about what a claim is called.
                bearer.MapInboundClaims = false;

                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = options.Issuer,
                    ValidateAudience = true,
                    ValidAudience = options.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(options.SigningKey)),
                    ValidateLifetime = true,

                    // Enough to absorb a little clock drift between machines, not enough to keep
                    // a lapsed session alive for long.
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = ClaimNames.Username,
                    RoleClaimType = ClaimNames.Role,
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(
                ModeratorPolicy,
                policy => policy.RequireRole(UserRole.Moderator.ToString()));

        return services;
    }
}
