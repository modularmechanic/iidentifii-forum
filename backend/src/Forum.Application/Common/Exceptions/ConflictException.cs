namespace Forum.Application.Common.Exceptions;

/// <summary>Raised when storing something would duplicate what is already there.</summary>
public sealed class ConflictException(
    string message,
    string? constraintName = null,
    Exception? innerException = null) : Exception(message, innerException)
{
    /// <summary>
    /// The database constraint that refused the write, where one is known. Registration needs it:
    /// a duplicate username may be reported, and a duplicate address may not.
    /// </summary>
    public string? ConstraintName { get; } = constraintName;
}
