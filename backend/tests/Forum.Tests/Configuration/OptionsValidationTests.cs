using FluentAssertions;
using Forum.Application;
using Forum.Application.Common.Options;
using Forum.Infrastructure;
using Forum.Infrastructure.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Forum.Tests.Configuration;

/// <summary>
/// Settings that cannot work are refused while the application is starting, so a bad deployment
/// fails at the moment it rolls out rather than the first time somebody tries to sign in.
/// </summary>
public sealed class OptionsValidationTests
{
    private const string Pepper = "configuration-test-pepper-value-long-enough";

    [Theory]
    [InlineData("Tokens:LinkLifetime", "00:00:00")]
    [InlineData("Tokens:LinkLifetime", "-01:00:00")]
    [InlineData("Tokens:CodeLifetime", "00:00:00")]
    [InlineData("Tokens:CodeLifetime", "-00:10:00")]
    [InlineData("Tokens:ResendCooldown", "00:00:00")]
    [InlineData("Tokens:ResendCooldown", "-00:01:00")]
    public void A_token_duration_that_is_not_positive_is_refused(string key, string value)
    {
        var read = () => ReadTokens(new() { ["Tokens:Pepper"] = Pepper, [key] = value });

        read.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void The_shipped_token_durations_are_accepted()
    {
        var read = () => ReadTokens(new() { ["Tokens:Pepper"] = Pepper });

        read.Should().NotThrow();
    }

    /// <summary>
    /// A username alone would authenticate with an empty password, and a password alone would
    /// never be sent. Both are silent, so neither is allowed to start.
    /// </summary>
    [Theory]
    [InlineData("smtp-user", null)]
    [InlineData(null, "smtp-secret")]
    public void Half_a_set_of_smtp_credentials_is_refused(string? username, string? password)
    {
        var read = () => ReadEmail(username, password);

        read.Should().Throw<OptionsValidationException>();
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("smtp-user", "smtp-secret")]
    public void Both_smtp_credentials_or_neither_are_accepted(string? username, string? password)
    {
        var read = () => ReadEmail(username, password);

        read.Should().NotThrow();
    }

    /// <summary>Reading the value runs the same validation the host runs before it serves anything.</summary>
    private static TokenOptions ReadTokens(Dictionary<string, string?> settings)
        => Build(services => services.AddForumApplication(Configuration(settings)))
            .GetRequiredService<IOptions<TokenOptions>>().Value;

    private static EmailOptions ReadEmail(string? username, string? password)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Forum"] = "Host=localhost;Database=forum;Username=forum;Password=forum",
            ["Email:Username"] = username,
            ["Email:Password"] = password,
        };

        return Build(services => services.AddForumInfrastructure(Configuration(settings)))
            .GetRequiredService<IOptions<EmailOptions>>().Value;
    }

    private static IConfiguration Configuration(Dictionary<string, string?> settings)
        => new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

    private static ServiceProvider Build(Action<IServiceCollection> register)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        register(services);

        return services.BuildServiceProvider();
    }
}
