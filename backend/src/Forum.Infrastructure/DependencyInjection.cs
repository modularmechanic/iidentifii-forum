using Forum.Application.Common.Interfaces;
using Forum.Domain.Users;
using Forum.Infrastructure.Auth;
using Forum.Infrastructure.Email;
using Forum.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Forum.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers persistence and the services that reach outside the process.</summary>
    public static IServiceCollection AddForumInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Forum")
            ?? throw new InvalidOperationException(
                "Connection string 'Forum' is missing. See docs/reference/configuration.md.");

        services.AddDbContext<ForumDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IForumDbContext>(provider => provider.GetRequiredService<ForumDbContext>());

        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<DbSeeder>();
        services.AddScoped<ITokenService, JwtTokenService>();

        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Without a mail server configured, messages go to the log so nothing is lost.
        if (configuration.GetValue($"{EmailOptions.SectionName}:Enabled", defaultValue: true))
        {
            services.AddScoped<IEmailTransport, SmtpEmailSender>();
        }
        else
        {
            services.AddScoped<IEmailTransport, LogEmailSender>();
        }

        // What the application calls returns as soon as the message is accepted; delivery happens
        // behind the response, so an address that exists cannot be told from one that does not by
        // how long the answer takes.
        services.AddSingleton<EmailQueue>();
        services.AddScoped<IEmailSender, QueuedEmailSender>();
        services.AddHostedService<EmailDispatcher>();

        return services;
    }
}
