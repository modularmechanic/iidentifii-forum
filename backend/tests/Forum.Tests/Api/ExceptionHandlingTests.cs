using FluentAssertions;
using Forum.Api.Errors;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using Xunit;

namespace Forum.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class ExceptionHandlingTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    /// <summary>
    /// A body the model binder cannot read used to be explained in the framework's own words,
    /// which name CLR types and count bytes. Neither says anything to whoever sent the request,
    /// and both describe the inside of the API.
    /// </summary>
    [Fact]
    public async Task An_unreadable_body_is_refused_without_framework_detail()
    {
        using var content = new StringContent(
            """{"challengeId":"not-a-guid","code":"123456"}""",
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/api/v1/auth/verify-2fa", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("Forum.Application")
            .And.NotContain("System.")
            .And.NotContain("LineNumber")
            .And.NotContain("BytePositionInLine");

        // Reported against the field the caller sent, not the offset the parser stopped on.
        body.Should().Contain("challengeId")
            .And.Contain("The value is not in a form this field accepts.");
    }

    /// <summary>A message the API wrote itself is still the one the caller sees.</summary>
    [Fact]
    public async Task A_field_the_API_checks_keeps_its_own_message()
    {
        var response = await _client.GetAsync("/api/v1/posts?page=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync())
            .Should().Contain("Page numbering starts at 1.");
    }

    /// <summary>
    /// Diagnostics are optional: when nothing is listening, no activity exists to read an
    /// identifier from, and the handler must still answer.
    /// </summary>
    [Fact]
    public async Task Failure_without_an_activity_still_returns_problem_details()
    {
        var context = BuildHttpContext();
        context.Features.Set<IHttpActivityFeature>(new StubActivityFeature(activity: null));

        var handled = await HandleAsync(context);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task Failure_with_an_activity_reports_its_identifier()
    {
        using var activity = new Activity("request").Start();
        var context = BuildHttpContext();
        context.Features.Set<IHttpActivityFeature>(new StubActivityFeature(activity));

        var handled = await HandleAsync(context);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    private static async Task<bool> HandleAsync(HttpContext context)
    {
        var handler = new GlobalExceptionHandler(
            context.RequestServices.GetRequiredService<IProblemDetailsService>(),
            NullLogger<GlobalExceptionHandler>.Instance);

        return await handler.TryHandleAsync(context, new InvalidOperationException("boom"), CancellationToken.None);
    }

    private static DefaultHttpContext BuildHttpContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProblemDetails();

        var context = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        context.Response.Body = new MemoryStream();
        return context;
    }

    private sealed class StubActivityFeature(Activity? activity) : IHttpActivityFeature
    {
        public Activity Activity { get; set; } = activity!;
    }
}
