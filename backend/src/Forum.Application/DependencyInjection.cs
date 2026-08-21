using Forum.Application.Common.Options;
using Forum.Application.Common.Security;
using Forum.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Forum.Application;

public static class DependencyInjection
{
    /// <summary>Registers the services that carry the forum's behaviour.</summary>
    public static IServiceCollection AddForumApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<PostService>();
        services.AddScoped<CommentService>();
        services.AddScoped<AuthService>();
        services.AddScoped<UserTokenService>();
        services.AddSingleton<SecretHasher>();
        services.AddSingleton(TimeProvider.System);

        // Validated when the application starts rather than when a request first needs them, so a
        // missing secret is a failure to boot instead of a failure to sign in.
        services.AddOptions<TokenOptions>()
            .Bind(configuration.GetSection(TokenOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<AppOptions>()
            .Bind(configuration.GetSection(AppOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
