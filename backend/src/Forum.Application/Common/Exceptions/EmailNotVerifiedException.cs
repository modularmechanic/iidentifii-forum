namespace Forum.Application.Common.Exceptions;

/// <summary>Raised when an account exists but is not yet allowed to sign in.</summary>
public sealed class EmailNotVerifiedException(string message) : Exception(message);
