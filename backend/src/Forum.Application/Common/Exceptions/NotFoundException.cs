namespace Forum.Application.Common.Exceptions;

/// <summary>Raised when the item a request names does not exist.</summary>
public sealed class NotFoundException(string message) : Exception(message)
{
    public static NotFoundException Discussion(Guid id) => new($"Discussion {id} was not found.");

    public static NotFoundException Reply(Guid id) => new($"Reply {id} was not found.");
}
