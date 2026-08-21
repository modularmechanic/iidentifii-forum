namespace Forum.Application.Dtos;

/// <summary>A signed-in session: the token to present, when it lapses, and who it belongs to.</summary>
public sealed record AuthenticatedResponse(string Token, DateTimeOffset ExpiresAt, UserDto User);
