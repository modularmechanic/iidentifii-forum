namespace Forum.Api.Contracts;

/// <summary>Liveness payload returned by <c>GET /health</c>.</summary>
public sealed record HealthResponse(string Status, string Environment, DateTimeOffset CheckedAt);
