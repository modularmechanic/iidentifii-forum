namespace Forum.Domain.Common;

/// <summary>Why a rule refused an operation. The API maps these to status codes.</summary>
public enum DomainError
{
    /// <summary>The request was understood but breaks a rule of the forum.</summary>
    RuleViolation,

    /// <summary>The caller is not allowed to act on this item.</summary>
    Forbidden,

    /// <summary>The item is already in the state the caller asked for.</summary>
    Conflict,
}

/// <summary>
/// Raised when an operation would break a rule that must always hold. Carries no HTTP
/// knowledge: translating it into a response is the API's job.
/// </summary>
public sealed class DomainException(string message, DomainError error) : Exception(message)
{
    public DomainError Error { get; } = error;
}
