using Microsoft.AspNetCore.Mvc.Testing;

namespace Forum.Tests;

/// <summary>Hosts the API in memory for tests that exercise the real HTTP pipeline.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
}
