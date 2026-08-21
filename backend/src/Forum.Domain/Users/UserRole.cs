namespace Forum.Domain.Users;

/// <summary>What a member is allowed to do beyond posting.</summary>
public enum UserRole
{
    /// <summary>Posts, replies and likes.</summary>
    Member,

    /// <summary>Everything a member can do, plus flagging content as misleading or false.</summary>
    Moderator,
}
