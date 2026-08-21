namespace Forum.Application.Common.Exceptions;

/// <summary>Raised when storing something would duplicate what is already there.</summary>
public sealed class ConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);
