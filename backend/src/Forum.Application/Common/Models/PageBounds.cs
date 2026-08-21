namespace Forum.Application.Common.Models;

/// <summary>Limits shared by every paged request.</summary>
public static class PageBounds
{
    public const int MaxPageSize = 100;

    /// <summary>
    /// The highest page anyone may ask for. Chosen so that skipping to it cannot overflow, which
    /// would otherwise turn a large page number into a negative offset and fail the request.
    /// </summary>
    public const int MaxPage = int.MaxValue / MaxPageSize;
}
