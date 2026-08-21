using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Forum.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Forum.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class PasswordResetTests(ApiFactory factory)
{
    private const string NewPassword = "ADifferentPassword456!";

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task The_emailed_link_sets_a_new_password()
    {
        var account = await RegisterAndConfirmAsync();

        await ForgotAsync(account.Email);
        var response = await ResetAsync(TokenFor(account.Email), NewPassword);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task The_new_password_works_and_the_old_one_does_not()
    {
        var account = await RegisterAndConfirmAsync();
        await ForgotAsync(account.Email);
        await ResetAsync(TokenFor(account.Email), NewPassword);

        var withOld = await LoginAsync(account.Username, account.Password);
        var withNew = await LoginAsync(account.Username, NewPassword);

        withOld.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        withNew.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task The_link_works_only_once()
    {
        var account = await RegisterAndConfirmAsync();
        await ForgotAsync(account.Email);
        var token = TokenFor(account.Email);

        await ResetAsync(token, NewPassword);
        var second = await ResetAsync(token, "YetAnotherPassword789!");

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await second.Content.ReadFromJsonAsync<ProblemDetails>(TestJson.Options);
        problem!.Detail.Should().Contain("already been used");
    }

    /// <summary>
    /// Somebody resetting a password may be recovering from a compromise, so a sign-in already
    /// under way must not survive the change.
    /// </summary>
    [Fact]
    public async Task A_sign_in_already_under_way_stops_working()
    {
        var account = await RegisterAndConfirmAsync();

        var challenge = await LoginAsync(account.Username, account.Password)
            .ContinueWith(task => task.Result.Content.ReadFromJsonAsync<LoginChallengeResponse>(TestJson.Options))
            .Unwrap();
        var code = CodeFor(account.Email);

        await ForgotAsync(account.Email);
        await ResetAsync(TokenFor(account.Email), NewPassword);

        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/verify-2fa",
            new VerifyTwoFactorRequest { ChallengeId = challenge!.ChallengeId, Code = code },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_link_left_too_long_stops_working()
    {
        var account = await RegisterAndConfirmAsync();
        await ForgotAsync(account.Email);
        var token = TokenFor(account.Email);

        factory.Clock.Advance(TimeSpan.FromHours(1) + TimeSpan.FromMinutes(1));

        var response = await ResetAsync(token, NewPassword);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_token_nobody_issued_is_refused()
    {
        var response = await ResetAsync("a-token-that-was-never-issued", NewPassword);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_new_password_shorter_than_the_minimum_is_refused()
    {
        var account = await RegisterAndConfirmAsync();
        await ForgotAsync(account.Email);

        var response = await ResetAsync(TokenFor(account.Email), "short");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJson.Options);
        problem!.Errors.Should().ContainKey("NewPassword");
    }

    /// <summary>The reply is the same whether or not the address belongs to an account.</summary>
    [Fact]
    public async Task Asking_for_an_address_nobody_registered_reports_the_same_thing()
    {
        var response = await ForgotAsync($"nobody-{Guid.CreateVersion7():N}@example.com");

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    private string TokenFor(string emailAddress)
        => factory.Emails.LastTokenTo(emailAddress)
            ?? throw new InvalidOperationException($"No reset link was sent to {emailAddress}.");

    private string CodeFor(string emailAddress)
    {
        // An empty code would be refused and the assertion would see the 401 it wanted, so this
        // test would pass whether or not a code was ever sent. Both the missing message and the
        // message without a code have to stop it instead.
        var body = factory.Emails.LastTo(emailAddress)?.PlainTextBody
            ?? throw new InvalidOperationException($"No message was sent to {emailAddress}.");

        var match = System.Text.RegularExpressions.Regex.Match(body, @"code is (\d{6})");

        return match.Success
            ? match.Groups[1].Value
            : throw new InvalidOperationException($"The message to {emailAddress} carried no code.");
    }

    private Task<HttpResponseMessage> ForgotAsync(string emailAddress)
        => _client.PostAsJsonAsync(
            "/api/v1/auth/forgot-password",
            new ForgotPasswordRequest { Email = emailAddress },
            TestJson.Options);

    private Task<HttpResponseMessage> ResetAsync(string token, string newPassword)
        => _client.PostAsJsonAsync(
            "/api/v1/auth/reset-password",
            new ResetPasswordRequest { Token = token, NewPassword = newPassword },
            TestJson.Options);

    private Task<HttpResponseMessage> LoginAsync(string username, string password)
        => _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest { Username = username, Password = password },
            TestJson.Options);

    private async Task<RegisterRequest> RegisterAndConfirmAsync()
    {
        var suffix = Guid.CreateVersion7().ToString("N")[..12];
        var request = new RegisterRequest
        {
            Username = $"member{suffix}",
            Email = $"member{suffix}@example.com",
            Password = "Password123!",
        };

        await _client.PostAsJsonAsync("/api/v1/auth/register", request, TestJson.Options);

        await _client.PostAsJsonAsync(
            "/api/v1/auth/verify-email",
            new VerifyEmailRequest { Token = TokenFor(request.Email) },
            TestJson.Options);

        return request;
    }
}
