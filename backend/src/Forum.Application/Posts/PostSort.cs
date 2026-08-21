namespace Forum.Application.Posts;

/// <summary>What the discussion list is ordered by.</summary>
public enum PostSort
{
    /// <summary>When the discussion was started.</summary>
    CreatedAt,

    /// <summary>How many people liked it.</summary>
    LikeCount,
}

/// <summary>Which end of the ordering comes first.</summary>
public enum SortOrder
{
    Ascending,

    Descending,
}
