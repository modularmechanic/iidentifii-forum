using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Forum.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Forum.Tests.Api;

public sealed class HealthEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_health_reports_the_service_as_healthy()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        body!.Status.Should().Be("healthy");
    }

    /// <summary>
    /// Reflection-free serialisation means framework error payloads must be covered by the
    /// serialiser context too. A missing route is the cheapest way to produce one.
    /// </summary>
    [Fact]
    public async Task Unknown_route_returns_problem_details()
    {
        var response = await _client.GetAsync("/does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Status.Should().Be((int)HttpStatusCode.NotFound);
    }
}
