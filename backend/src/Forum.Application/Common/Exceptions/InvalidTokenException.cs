namespace Forum.Application.Common.Exceptions;

/// <summary>Raised when a one-time secret is unknown, expired or already spent.</summary>
public sealed class InvalidTokenException(string message) : Exception(message);
