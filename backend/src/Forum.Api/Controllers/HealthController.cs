using Forum.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Forum.Api.Controllers;

/// <summary>Liveness probe used by Docker, the front-end shell and uptime checks.</summary>
[ApiController]
[Route("health")]
public sealed class HealthController(IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> Get()
        => Ok(new HealthResponse("healthy", environment.EnvironmentName, DateTimeOffset.UtcNow));
}
