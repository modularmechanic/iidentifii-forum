namespace Forum.Application.Common.Exceptions;

/// <summary>Raised when a sign-in attempt is refused.</summary>
public sealed class AuthenticationException(string message) : Exception(message);
