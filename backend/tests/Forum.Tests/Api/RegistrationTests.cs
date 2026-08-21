using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Forum.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Forum.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class RegistrationTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Registering_creates_an_account_and_sends_a_verification_link()
    {
        var account = NewAccount();

        var response = await RegisterAsync(account);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var message = factory.Emails.LastTo(account.Email);
        message.Should().NotBeNull();
        message!.Subject.Should().Be("Confirm your email address");
        message.PlainTextBody.Should().Contain("verify-email?token=");
    }

    [Fact]
    public async Task A_username_already_taken_is_refused()
    {
        var first = NewAccount();
        await RegisterAsync(first);

        var response = await RegisterAsync(first with { Email = $"other-{first.Email}" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestJson.Options);
        problem!.Status.Should().Be(409);
    }

    [Fact]
    public async Task An_address_already_registered_is_refused()
    {
        var first = NewAccount();
        await RegisterAsync(first);

        var response = await RegisterAsync(first with { Username = $"other{Suffix()}" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    /// <summary>Usernames and addresses are compared without regard to case, so neither can be reused.</summary>
    [Fact]
    public async Task A_username_differing_only_in_case_is_refused()
    {
        var first = NewAccount();
        await RegisterAsync(first);

        var response = await RegisterAsync(first with
        {
            Username = first.Username.ToUpperInvariant(),
            Email = $"other-{first.Email}",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("ab", "Username too short")]
    [InlineData("has spaces", "Username has spaces")]
    [InlineData("has-a-dash", "Username has punctuation")]
    public async Task An_unusable_username_is_refused(string username, string _)
    {
        var response = await RegisterAsync(NewAccount() with { Username = username });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJson.Options);
        problem!.Errors.Should().ContainKey("Username");
    }

    [Fact]
    public async Task An_address_that_is_not_an_address_is_refused()
    {
        var response = await RegisterAsync(NewAccount() with { Email = "not-an-address" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJson.Options);
        problem!.Errors.Should().ContainKey("Email");
    }

    [Fact]
    public async Task A_password_shorter_than_the_minimum_is_refused()
    {
        var response = await RegisterAsync(NewAccount() with { Password = "short" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJson.Options);
        problem!.Errors.Should().ContainKey("Password");
    }

    /// <summary>A refused registration must leave nothing behind that would block a second attempt.</summary>
    [Fact]
    public async Task A_refused_registration_leaves_the_username_free()
    {
        var account = NewAccount();

        await RegisterAsync(account with { Password = "short" });
        var response = await RegisterAsync(account);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private Task<HttpResponseMessage> RegisterAsync(RegisterRequest request)
        => _client.PostAsJsonAsync("/api/v1/auth/register", request, TestJson.Options);

    private static RegisterRequest NewAccount()
    {
        var suffix = Suffix();
        return new RegisterRequest
        {
            Username = $"member{suffix}",
            Email = $"member{suffix}@example.com",
            Password = "Password123!",
        };
    }

    /// <summary>Keeps each test's account distinct, so the suite can be run repeatedly.</summary>
    private static string Suffix() => Guid.CreateVersion7().ToString("N")[..12];
}
