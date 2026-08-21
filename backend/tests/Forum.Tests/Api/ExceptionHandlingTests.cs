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
using Xunit;

namespace Forum.Tests.Api;

public sealed class ExceptionHandlingTests
{
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
