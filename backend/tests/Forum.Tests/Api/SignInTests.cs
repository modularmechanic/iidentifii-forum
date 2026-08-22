using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Forum.Application.Dtos;
using Forum.Domain.Users;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Forum.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class SignInTests(ApiFactory factory)
{
    private const string SeedPassword = "Password123!";

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task A_password_alone_does_not_sign_anybody_in()
    {
        var account = await RegisterAndConfirmAsync();

        var response = await LoginAsync(account.Username, account.Password);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var challenge = await response.Content.ReadFromJsonAsync<LoginChallengeResponse>(TestJson.Options);
        challenge!.ChallengeId.Should().NotBeEmpty();
        challenge.MaskedEmail.Should().NotBe(account.Email, "the address should not be repeated back in full");
        challenge.MaskedEmail.Should().Contain("@").And.Contain("*");
    }

    [Fact]
    public async Task The_emailed_code_completes_the_sign_in()
    {
        var account = await RegisterAndConfirmAsync();

        var session = await SignInFullyAsync(account);

        session.Token.Should().NotBeEmpty();
        session.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
        session.User.Username.Should().Be(account.Username);
    }

    [Fact]
    public async Task The_session_identifies_the_member_on_later_requests()
    {
        var account = await RegisterAndConfirmAsync();
        var session = await SignInFullyAsync(account);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var me = await response.Content.ReadFromJsonAsync<UserDto>(TestJson.Options);
        me!.Username.Should().Be(account.Username);
    }

    [Fact]
    public async Task Without_a_session_the_caller_is_anonymous()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_made_up_session_is_refused()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.real.token");
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_wrong_password_is_refused()
    {
        var account = await RegisterAndConfirmAsync();

        var response = await LoginAsync(account.Username, "not-the-password");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// A wrong username and a wrong password are answered the same way, so neither can be used to
    /// find out which accounts exist.
    /// </summary>
    [Fact]
    public async Task An_unknown_username_is_refused_the_same_way_as_a_wrong_password()
    {
        var account = await RegisterAndConfirmAsync();

        var wrongPassword = await LoginAsync(account.Username, "not-the-password");
        var unknownUser = await LoginAsync($"nobody{Guid.CreateVersion7():N}"[..20], "not-the-password");

        unknownUser.StatusCode.Should().Be(wrongPassword.StatusCode);

        var first = await wrongPassword.Content.ReadFromJsonAsync<ProblemDetails>(TestJson.Options);
        var second = await unknownUser.Content.ReadFromJsonAsync<ProblemDetails>(TestJson.Options);
        second!.Detail.Should().Be(first!.Detail);
    }

    [Fact]
    public async Task An_account_that_has_not_confirmed_its_address_cannot_sign_in()
    {
        var account = await RegisterAsync();

        var response = await LoginAsync(account.Username, account.Password);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestJson.Options);
        problem!.Detail.Should().Contain("Confirm your email");
    }

    [Fact]
    public async Task A_wrong_code_is_refused()
    {
        var account = await RegisterAndConfirmAsync();
        var challenge = await BeginSignInAsync(account);

        var response = await VerifyCodeAsync(challenge.ChallengeId, "000000");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>Guessing runs out rather than continuing, since a six-digit code is short.</summary>
    [Fact]
    public async Task Enough_wrong_codes_spend_the_attempt_entirely()
    {
        var account = await RegisterAndConfirmAsync();
        var challenge = await BeginSignInAsync(account);
        var code = CodeFor(account.Email);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await VerifyCodeAsync(challenge.ChallengeId, "000000");
        }

        var withTheRightCode = await VerifyCodeAsync(challenge.ChallengeId, code);

        withTheRightCode.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "the challenge should be spent even though this code is the right one");
    }

    [Fact]
    public async Task A_code_left_too_long_stops_working()
    {
        var account = await RegisterAndConfirmAsync();
        var challenge = await BeginSignInAsync(account);
        var code = CodeFor(account.Email);

        factory.Clock.Advance(TimeSpan.FromMinutes(11));

        var response = await VerifyCodeAsync(challenge.ChallengeId, code);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_code_cannot_be_used_twice()
    {
        var account = await RegisterAndConfirmAsync();
        var challenge = await BeginSignInAsync(account);
        var code = CodeFor(account.Email);

        await VerifyCodeAsync(challenge.ChallengeId, code);
        var second = await VerifyCodeAsync(challenge.ChallengeId, code);

        second.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_malformed_code_is_refused_before_anything_is_looked_up()
    {
        var response = await VerifyCodeAsync(Guid.CreateVersion7(), "abc");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJson.Options);
        problem!.Errors.Should().ContainKey("Code");
    }

    /// <summary>A seeded moderator signs in the same way, and the session says what they are.</summary>
    [Fact]
    public async Task A_moderator_session_carries_their_role()
    {
        var challenge = await _client
            .PostAsJsonAsync("/api/v1/auth/login", new LoginRequest { Username = "mod", Password = SeedPassword }, TestJson.Options)
            .ContinueWith(task => task.Result.Content.ReadFromJsonAsync<LoginChallengeResponse>(TestJson.Options))
            .Unwrap();

        var code = CodeFor("mod@forum.local");
        var response = await VerifyCodeAsync(challenge!.ChallengeId, code);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = await response.Content.ReadFromJsonAsync<AuthenticatedResponse>(TestJson.Options);
        session!.User.Role.Should().Be(UserRole.Moderator);
    }

    [Fact]
    public async Task Two_sign_ins_arriving_together_are_both_answered()
    {
        var account = await RegisterAndConfirmAsync();

        // Only one unspent token may exist per account, so these race in the database. Somebody
        // signing in from two tabs has done nothing wrong and must not be shown a conflict.
        var responses = await Task.WhenAll(
            LoginAsync(account.Username, account.Password),
            LoginAsync(account.Username, account.Password));

        responses.Should().OnlyContain(
            response => response.StatusCode == HttpStatusCode.OK,
            "a race between two of your own sign-ins is not the caller's problem");
    }

    [Fact]
    public async Task Only_the_newest_code_works_when_two_are_asked_for()
    {
        var account = await RegisterAndConfirmAsync();

        var first = await BeginSignInAsync(account);
        var firstCode = CodeFor(account.Email);

        var second = await BeginSignInAsync(account);
        var secondCode = CodeFor(account.Email);

        firstCode.Should().NotBe(secondCode, "asking again issues a new code");

        // Asking for a second code retires the first, so a code left in an older message cannot
        // be used afterwards. This is the point of retiring rather than accumulating.
        (await VerifyCodeAsync(first.ChallengeId, firstCode)).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);

        (await VerifyCodeAsync(second.ChallengeId, secondCode)).StatusCode
            .Should().Be(HttpStatusCode.OK);
    }

    private string CodeFor(string emailAddress)
    {
        var body = factory.Emails.LastTo(emailAddress)?.PlainTextBody
            ?? throw new InvalidOperationException($"No message was sent to {emailAddress}.");

        // An empty code is refused, which is what several of these tests expect anyway — so a
        // message carrying no code would let them pass without the code ever being sent.
        var match = Regex.Match(body, @"code is (\d{6})");

        return match.Success
            ? match.Groups[1].Value
            : throw new InvalidOperationException($"The message to {emailAddress} carried no code.");
    }

    private async Task<AuthenticatedResponse> SignInFullyAsync(RegisterRequest account)
    {
        var challenge = await BeginSignInAsync(account);
        var response = await VerifyCodeAsync(challenge.ChallengeId, CodeFor(account.Email));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<AuthenticatedResponse>(TestJson.Options))!;
    }

    private async Task<LoginChallengeResponse> BeginSignInAsync(RegisterRequest account)
    {
        var response = await LoginAsync(account.Username, account.Password);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return (await response.Content.ReadFromJsonAsync<LoginChallengeResponse>(TestJson.Options))!;
    }

    private Task<HttpResponseMessage> LoginAsync(string username, string password)
        => _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest { Username = username, Password = password },
            TestJson.Options);

    private Task<HttpResponseMessage> VerifyCodeAsync(Guid challengeId, string code)
        => _client.PostAsJsonAsync(
            "/api/v1/auth/verify-2fa",
            new VerifyTwoFactorRequest { ChallengeId = challengeId, Code = code },
            TestJson.Options);

    private async Task<RegisterRequest> RegisterAsync()
    {
        var suffix = Guid.CreateVersion7().ToString("N")[..12];
        var request = new RegisterRequest
        {
            Username = $"member{suffix}",
            Email = $"member{suffix}@example.com",
            Password = "Password123!",
        };

        (await _client.PostAsJsonAsync("/api/v1/auth/register", request, TestJson.Options))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        return request;
    }

    private async Task<RegisterRequest> RegisterAndConfirmAsync()
    {
        var account = await RegisterAsync();
        var token = factory.Emails.LastTokenTo(account.Email)!;

        await _client.PostAsJsonAsync(
            "/api/v1/auth/verify-email",
            new VerifyEmailRequest { Token = token },
            TestJson.Options);

        return account;
    }
}
