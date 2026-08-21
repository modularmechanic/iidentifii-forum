using Forum.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Forum", _database.GetConnectionString());
        builder.UseSetting("Database:SeedOnStartup", "true");
        builder.UseEnvironment("Development");
    }
}

/// <summary>Shares one container across every integration test class.</summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
