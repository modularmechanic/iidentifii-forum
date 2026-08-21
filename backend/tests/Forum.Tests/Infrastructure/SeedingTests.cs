using FluentAssertions;
using Forum.Domain.Users;
using Forum.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;
using Xunit;

namespace Forum.Tests.Infrastructure;

public sealed class SeedingTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("forum")
        .WithUsername("forum")
        .WithPassword("forum")
        .Build();

    public Task InitializeAsync() => _database.StartAsync();

    public Task DisposeAsync() => _database.DisposeAsync().AsTask();

    [Fact]
    public async Task Seeding_fills_an_empty_database_once_and_only_once()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var seeder = CreateSeeder(context);

        await seeder.SeedAsync();
        var afterFirstRun = await context.Users.CountAsync();

        await seeder.SeedAsync();
        var afterSecondRun = await context.Users.CountAsync();

        afterFirstRun.Should().Be(4);
        afterSecondRun.Should().Be(afterFirstRun);
        (await context.Posts.CountAsync()).Should().Be(20);
        (await context.PostTags.CountAsync()).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Seeded_accounts_can_have_their_password_verified()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await CreateSeeder(context).SeedAsync();

        var alice = await context.Users.SingleAsync(user => user.Username == "alice");
        var hasher = new PasswordHasher<User>();

        hasher.VerifyHashedPassword(alice, alice.PasswordHash, DbSeeder.SeedPassword)
            .Should().NotBe(PasswordVerificationResult.Failed);
        alice.IsEmailVerified.Should().BeTrue();
    }

    /// <summary>
    /// The seeded accounts share a published password, so a non-development environment must be
    /// refused even when the configuration switch asks for content.
    /// </summary>
    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task Seeding_is_refused_outside_development(string environmentName)
    {
        using (var host = BuildHost(environmentName))
        {
            await host.InitialiseDatabaseAsync(seed: true);
        }

        await using var context = CreateContext();
        (await context.Users.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Seeding_runs_in_development()
    {
        using (var host = BuildHost("Development"))
        {
            await host.InitialiseDatabaseAsync(seed: true);
        }

        await using var context = CreateContext();
        (await context.Users.CountAsync()).Should().Be(4);
    }

    private ForumDbContext CreateContext()
        => new(new DbContextOptionsBuilder<ForumDbContext>()
            .UseNpgsql(_database.GetConnectionString())
            .Options);

    private static DbSeeder CreateSeeder(ForumDbContext context)
        => new(context, new PasswordHasher<User>(), NullLogger<DbSeeder>.Instance);

    /// <summary>
    /// Builds the smallest host database initialisation needs. It owns its own context, because
    /// initialisation disposes the scope it works in.
    /// </summary>
    private IHost BuildHost(string environmentName)
        => new HostBuilder()
            .UseEnvironment(environmentName)
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddDbContext<ForumDbContext>(options =>
                    options.UseNpgsql(_database.GetConnectionString()));
                services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
                services.AddScoped<DbSeeder>();
            })
            .Build();
}
