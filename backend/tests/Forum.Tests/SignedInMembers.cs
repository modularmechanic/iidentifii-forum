using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Forum.Application.Dtos;

namespace Forum.Tests;

/// <summary>
/// Creates members that are ready to act: registered, confirmed, signed in, and holding the
/// bearer token every write needs. Each step is exercised on its own elsewhere; here they are
/// only the price of having somebody who can post.
/// </summary>
internal sealed class SignedInMembers(ApiFactory factory, HttpClient client)
{
    /// <summary>A member who can post, reply and like.</summary>
    public async Task<Member> CreateAsync()
    {
        var account = await RegisterAndConfirmAsync();
        var session = await SignInAsync(account);

        return new Member(session.User.Id, account.Username, session.Token);
    }

    /// <summary>Registers an account and confirms its address using the emailed link.</summary>
    public async Task<RegisterRequest> RegisterAndConfirmAsync()
    {
        var suffix = Guid.CreateVersion7().ToString("N")[..12];
        var account = new RegisterRequest
        {
            Username = $"writer{suffix}",
            Email = $"writer{suffix}@example.com",
            Password = "Password123!",
        };

        (await client.PostAsJsonAsync("/api/v1/auth/register", account, TestJson.Options))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var token = factory.Emails.LastTokenTo(account.Email)
            ?? throw new InvalidOperationException($"No confirmation was sent to {account.Email}.");

        (await client.PostAsJsonAsync(
            "/api/v1/auth/verify-email",
            new VerifyEmailRequest { Token = token },
            TestJson.Options)).StatusCode.Should().Be(HttpStatusCode.OK);

        return account;
    }

    private async Task<AuthenticatedResponse> SignInAsync(RegisterRequest account)
    {
        var challengeResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest { Username = account.Username, Password = account.Password },
            TestJson.Options);

        challengeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var challenge = await challengeResponse.Content
            .ReadFromJsonAsync<LoginChallengeResponse>(TestJson.Options);

        var body = factory.Emails.LastTo(account.Email)?.PlainTextBody
            ?? throw new InvalidOperationException($"No code was sent to {account.Email}.");
        var code = Regex.Match(body, @"code is (\d{6})").Groups[1].Value;

        var sessionResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/verify-2fa",
            new VerifyTwoFactorRequest { ChallengeId = challenge!.ChallengeId, Code = code },
            TestJson.Options);

        sessionResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        return (await sessionResponse.Content
            .ReadFromJsonAsync<AuthenticatedResponse>(TestJson.Options))!;
    }
}

/// <summary>A member with a session, and the means to make a request as them.</summary>
internal sealed record Member(Guid Id, string Username, string Token)
{
    public HttpRequestMessage Request(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);

        return request;
    }

    public HttpRequestMessage Request<TBody>(HttpMethod method, string path, TBody body)
    {
        var request = Request(method, path);
        request.Content = JsonContent.Create(body, options: TestJson.Options);

        return request;
    }
}
