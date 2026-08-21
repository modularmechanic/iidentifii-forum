using Forum.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace Forum.Application.Common.Security;

/// <summary>
/// A hash of nothing, used when no account matches a username. Verifying against it costs the same
/// as verifying a real one, so a wrong username and a wrong password cannot be told apart by how
/// long the answer takes.
/// </summary>
public static class EmptyPasswordHash
{
    public static string Value { get; } = new PasswordHasher<User>()
        .HashPassword(
            User.Register("placeholder", "placeholder@forum.local", "hash", DateTimeOffset.UnixEpoch),
            Guid.CreateVersion7().ToString());
}
