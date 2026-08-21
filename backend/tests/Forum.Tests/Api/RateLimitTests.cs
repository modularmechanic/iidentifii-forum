using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Forum.Application.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Forum.Tests.Api;

/// <summary>
/// Runs against its own host with a deliberately low limit, because the shared host raises the
/// limit so the rest of the suite is not throttled by itself.
/// </summary>
public sealed class RateLimitTests : IAsyncLifetime
{
    private const int Limit = 3;

    private readonly ApiFactory _factory = new();
    private WebApplicationFactory<Program> _throttled = null!;

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();

        _throttled = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("RateLimiting:AuthenticationPermitsPerMinute", Limit.ToString()));
    }

    public async Task DisposeAsync()
    {
        await _throttled.DisposeAsync();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Too_many_attempts_are_refused_and_told_when_to_return()
    {
        var client = _throttled.CreateClient();

        var accepted = 0;
        HttpResponseMessage? refused = null;

        for (var attempt = 0; attempt <= Limit; attempt++)
        {
            var response = await ResendAsync(client);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                refused = response;
                break;
            }

            accepted++;
        }

        accepted.Should().Be(Limit, "the limit should allow exactly that many");
        refused.Should().NotBeNull();
        refused!.Headers.RetryAfter.Should().NotBeNull("a caller needs to know when to try again");
        refused.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    /// <summary>Reading the forum is not limited as tightly as attempting to sign in.</summary>
    [Fact]
    public async Task Reading_is_unaffected_by_the_authentication_limit()
    {
        var client = _throttled.CreateClient();

        for (var attempt = 0; attempt < Limit + 2; attempt++)
        {
            await ResendAsync(client);
        }

        var response = await client.GetAsync("/api/v1/posts?pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static Task<HttpResponseMessage> ResendAsync(HttpClient client)
        => client.PostAsJsonAsync(
            "/api/v1/auth/resend-verification",
            new ResendVerificationRequest { Email = $"nobody-{Guid.CreateVersion7():N}@example.com" },
            TestJson.Options);
}
