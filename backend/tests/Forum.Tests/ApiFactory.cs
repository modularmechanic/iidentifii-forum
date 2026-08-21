using Forum.Application.Common.Interfaces;
using Forum.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Testcontainers.PostgreSql;
using Xunit;

namespace Forum.Tests;

/// <summary>
/// Hosts the API against a throwaway PostgreSQL container, so tests exercise the real provider,
/// the real migration and the real seed data rather than a substitute.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("forum")
        .WithUsername("forum")
        .WithPassword("forum")
        .Build();

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        // Touching the client applies migrations and seeds through the normal startup path.
        using var scope = Services.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<ForumDbContext>();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }

    /// <summary>The messages the API tried to send during a test.</summary>
    public FakeEmailSender Emails { get; } = new();

    /// <summary>
    /// The clock the API reads. Held by the test so waiting out a cooldown or an expiry is a
    /// method call rather than a real pause.
    /// </summary>
    public FakeTimeProvider Clock { get; } = new(DateTimeOffset.UtcNow);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Forum", _database.GetConnectionString());
        builder.UseSetting("Database:SeedOnStartup", "true");
        builder.UseSetting("Tokens:Pepper", "integration-test-pepper-value-long-enough");
        builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-long-enough-to-pass");
        builder.UseSetting("App:PublicUrl", "http://localhost:5173");

        // Every test shares one client address, so the production limit would be reached by the
        // suite itself. RateLimitTests sets its own low limit to exercise the behaviour.
        builder.UseSetting("RateLimiting:AuthenticationPermitsPerMinute", "10000");
        builder.UseSetting("RateLimiting:GlobalPermitsPerMinute", "10000");
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }
}

/// <summary>Shares one container across every integration test class.</summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
