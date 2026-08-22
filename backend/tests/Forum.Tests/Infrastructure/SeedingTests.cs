using FluentAssertions;
using Forum.Domain.Users;
using Forum.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
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

        afterFirstRun.Should().Be(11);
        afterSecondRun.Should().Be(afterFirstRun);
        (await context.Posts.CountAsync()).Should().Be(20);
        (await context.PostTags.CountAsync()).Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Two instances starting together can both find the table empty and both try to write. The
    /// unique index on usernames decides which one wins, and the loser must finish quietly rather
    /// than bringing the application down. Entity Framework saves in one transaction, so the
    /// loser's rows roll back whole and the database is left with exactly one set of content.
    /// </summary>
    [Fact]
    public async Task Two_instances_seeding_at_once_leave_one_set_of_content()
    {
        await using var schema = CreateContext();
        await schema.Database.MigrateAsync();

        await using var firstContext = CreateContext();
        await using var secondContext = CreateContext();

        var log = new RecordingLogger();
        var first = new DbSeeder(firstContext, new PasswordHasher<User>(), log);
        var second = new DbSeeder(secondContext, new PasswordHasher<User>(), log);

        // Released together, so both are past their empty-database check before either writes.
        var startingGun = new TaskCompletionSource();
        var races = new[]
        {
            Task.Run(async () => { await startingGun.Task; await first.SeedAsync(); }),
            Task.Run(async () => { await startingGun.Task; await second.SeedAsync(); }),
        };

        startingGun.SetResult();

        var racing = async () => await Task.WhenAll(races);
        await racing.Should().NotThrowAsync();

        // Asserted as invariants rather than exact totals, so the test still means the same thing
        // when the sample content changes: one set of content, with nothing written twice.
        await using var verification = CreateContext();
        var usernames = await verification.Users.Select(user => user.Username).ToListAsync();
        var titles = await verification.Posts.Select(post => post.Title).ToListAsync();

        log.Entries.Should().Contain(entry => entry.Message.Contains("seeded the database first"),
            "the losing instance must take the conflict path this test exists to cover");
        usernames.Should().NotBeEmpty().And.OnlyHaveUniqueItems();
        titles.Should().NotBeEmpty().And.OnlyHaveUniqueItems();
        (await verification.Comments.CountAsync())
            .Should().Be(await verification.Comments.Select(c => c.Id).Distinct().CountAsync());
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
    /// refused even when the configuration switch asks for content — and must say so, or an
    /// operator is left believing the switch worked. Started the way the application starts, so
    /// the decision is taken by the code under test rather than by the test.
    /// </summary>
    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task Seeding_is_refused_outside_development(string environmentName)
    {
        var log = new RecordingLogger();

        await using (var api = new Api(
            _database.GetConnectionString(), environmentName, seedOnStartup: true, log))
        {
            // The host is built on first use, and starting it runs database initialisation. The
            // connection is read back because an empty table only means a refusal if the host was
            // looking at this test's database rather than the one in appsettings.json.
            using var scope = api.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<ForumDbContext>().Database.GetConnectionString()
                .Should().Be(_database.GetConnectionString());
        }

        await using var context = CreateContext();
        (await context.Users.CountAsync()).Should().Be(0);
        log.Entries.Should().Contain(
            entry => entry.Level == LogLevel.Warning
                && entry.Message.Contains("Seeding was requested but refused")
                && entry.Message.Contains(environmentName),
            "an operator who asks for seeding needs to read why it did not happen");
    }

    /// <summary>
    /// The warning reports a refusal, so leaving the switch off — the normal case everywhere but
    /// a developer's machine — has nothing to report and must stay quiet.
    /// </summary>
    [Fact]
    public async Task Seeding_nobody_asked_for_is_not_reported()
    {
        var log = new RecordingLogger();

        await using (var api = new Api(
            _database.GetConnectionString(), "Production", seedOnStartup: false, log))
        {
            using var scope = api.Services.CreateScope();
            _ = scope.ServiceProvider.GetRequiredService<ForumDbContext>();
        }

        await using var context = CreateContext();
        (await context.Users.CountAsync()).Should().Be(0);
        log.Entries.Should().NotContain(entry => entry.Message.Contains("Seeding was requested"));
    }

    [Fact]
    public async Task Seeding_runs_in_development()
    {
        using (var host = BuildHost("Development"))
        {
            await host.InitialiseDatabaseAsync(seed: true);
        }

        await using var context = CreateContext();
        (await context.Users.CountAsync()).Should().Be(11);
    }

    /// <summary>
    /// Captures what was logged, at what level, so a test can prove which path the code took.
    /// Handed to a seeder directly, or registered with a host as the provider behind every
    /// category.
    /// </summary>
    private sealed class RecordingLogger : ILogger<DbSeeder>, ILoggerProvider
    {
        private readonly List<(LogLevel Level, string Message)> _entries = [];

        public IReadOnlyList<(LogLevel Level, string Message)> Entries
        {
            get { lock (_entries) { return _entries.ToList(); } }
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public ILogger CreateLogger(string categoryName) => this;

        public void Dispose()
        {
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            lock (_entries)
            {
                _entries.Add((logLevel, formatter(state, exception)));
            }
        }
    }

    private ForumDbContext CreateContext()
        => new(new DbContextOptionsBuilder<ForumDbContext>()
            .UseNpgsql(_database.GetConnectionString())
            .Options);

    private static DbSeeder CreateSeeder(ForumDbContext context)
        => new(context, new PasswordHasher<User>(), NullLogger<DbSeeder>.Instance);

    /// <summary>
    /// Hosts the API against this test's database in a named environment, with the seeding switch
    /// on, so the refusal is decided by the same call path a deployment takes. Development makes
    /// its own secrets; every other environment expects them, so they are supplied here.
    /// </summary>
    private sealed class Api(
        string connectionString,
        string environmentName,
        bool seedOnStartup,
        ILoggerProvider log) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environmentName);
            builder.UseSetting("ConnectionStrings:Forum", connectionString);
            builder.UseSetting("Database:SeedOnStartup", seedOnStartup.ToString());
            builder.UseSetting("Tokens:Pepper", "seeding-test-pepper-value-long-enough");
            builder.UseSetting("Jwt:SigningKey", "seeding-test-signing-key-long-enough-to-pass");
            builder.ConfigureLogging(logging => logging.AddProvider(log));
        }
    }

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
