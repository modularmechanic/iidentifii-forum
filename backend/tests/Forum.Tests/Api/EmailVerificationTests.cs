using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Forum.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Forum.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class EmailVerificationTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task The_emailed_link_confirms_the_address()
    {
        var account = await RegisterAsync();

        var response = await VerifyAsync(TokenFor(account.Email));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task The_link_works_only_once()
    {
        var account = await RegisterAsync();
        var token = TokenFor(account.Email);

        await VerifyAsync(token);
        var second = await VerifyAsync(token);

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await second.Content.ReadFromJsonAsync<ProblemDetails>(TestJson.Options);
        problem!.Detail.Should().Contain("already been used");
    }

    [Fact]
    public async Task A_token_nobody_issued_is_refused()
    {
        var response = await VerifyAsync("a-token-that-was-never-issued");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task An_empty_token_is_refused_before_anything_is_looked_up()
    {
        var response = await VerifyAsync(string.Empty);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJson.Options);
        problem!.Errors.Should().ContainKey("Token");
    }

    /// <summary>Only the newest link works, so a link that was replaced cannot be used later.</summary>
    [Fact]
    public async Task Asking_again_retires_the_previous_link()
    {
        var account = await RegisterAsync();
        var firstToken = TokenFor(account.Email);

        factory.Clock.Advance(TimeSpan.FromMinutes(2));
        await ResendAsync(account.Email);
        var secondToken = TokenFor(account.Email);
        secondToken.Should().NotBe(firstToken);

        (await VerifyAsync(firstToken)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await VerifyAsync(secondToken)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_link_left_too_long_stops_working()
    {
        var account = await RegisterAsync();
        var token = TokenFor(account.Email);

        factory.Clock.Advance(TimeSpan.FromHours(1) + TimeSpan.FromMinutes(1));

        var response = await VerifyAsync(token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestJson.Options);
        problem!.Detail.Should().Contain("expired");
    }

    [Fact]
    public async Task A_link_still_within_its_hour_works()
    {
        var account = await RegisterAsync();
        var token = TokenFor(account.Email);

        factory.Clock.Advance(TimeSpan.FromMinutes(59));

        (await VerifyAsync(token)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Asking_again_once_the_cooldown_has_passed_sends_another()
    {
        var account = await RegisterAsync();
        var before = factory.Emails.Sent.Count;

        factory.Clock.Advance(TimeSpan.FromMinutes(2));
        var response = await ResendAsync(account.Email);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        factory.Emails.Sent.Count.Should().Be(before + 1);
    }

    [Fact]
    public async Task Asking_again_too_soon_sends_nothing_but_still_reports_success()
    {
        var account = await RegisterAsync();
        var before = factory.Emails.Sent.Count;

        var response = await ResendAsync(account.Email);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        factory.Emails.Sent.Count.Should().Be(before, "the cooldown had not passed");
    }

    /// <summary>
    /// The reply is the same whether or not the address belongs to an account, so it cannot be
    /// used to learn who is registered.
    /// </summary>
    [Fact]
    public async Task Asking_for_an_address_nobody_registered_reports_the_same_thing()
    {
        var response = await ResendAsync($"nobody-{Guid.CreateVersion7():N}@example.com");

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Asking_again_for_an_address_already_confirmed_sends_nothing()
    {
        var account = await RegisterAsync();
        await VerifyAsync(TokenFor(account.Email));
        var before = factory.Emails.Sent.Count;

        var response = await ResendAsync(account.Email);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        factory.Emails.Sent.Count.Should().Be(before);
    }

    private string TokenFor(string emailAddress)
        => factory.Emails.LastTokenTo(emailAddress)
            ?? throw new InvalidOperationException($"No verification link was sent to {emailAddress}.");

    private async Task<RegisterRequest> RegisterAsync()
    {
        var suffix = Guid.CreateVersion7().ToString("N")[..12];
        var request = new RegisterRequest
        {
            Username = $"member{suffix}",
            Email = $"member{suffix}@example.com",
            Password = "Password123!",
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request, TestJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return request;
    }

    private Task<HttpResponseMessage> VerifyAsync(string token)
        => _client.PostAsJsonAsync(
            "/api/v1/auth/verify-email",
            new VerifyEmailRequest { Token = token },
            TestJson.Options);

    private Task<HttpResponseMessage> ResendAsync(string emailAddress)
        => _client.PostAsJsonAsync(
            "/api/v1/auth/resend-verification",
            new ResendVerificationRequest { Email = emailAddress },
            TestJson.Options);
}
