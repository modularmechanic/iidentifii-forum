using Forum.Application.Common.Exceptions;
using Forum.Domain.Posts;
using Forum.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Forum.Infrastructure.Persistence;

/// <summary>
/// Fills an empty database with content an assessor can explore straight away. It is deliberately
/// deterministic and does nothing at all once any user exists, so restarts never duplicate it.
/// </summary>
public sealed class DbSeeder(
    ForumDbContext database,
    IPasswordHasher<User> passwordHasher,
    ILogger<DbSeeder> logger)
{
    /// <summary>Shared by every seeded account. Documented in the README; development only.</summary>
    public const string SeedPassword = "Password123!";

    private static readonly string[] MemberUsernames = ["alice", "bob", "carol"];
    private const string ModeratorUsername = "mod";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await database.Users.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Database already holds content; skipping seed.");
            return;
        }

        // A fixed clock keeps timestamps stable relative to each other across runs.
        var now = DateTimeOffset.UtcNow;

        var users = CreateUsers(now);
        database.Users.AddRange(users.Values);

        var posts = CreatePosts(users, now);
        database.Posts.AddRange(posts);

        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (ConflictException)
        {
            // Two instances starting together can both find the table empty. The unique index on
            // usernames decides which one wins; the other simply has nothing left to do.
            logger.LogInformation("Another instance seeded the database first; nothing to do.");
            return;
        }

        logger.LogInformation(
            "Seeded {UserCount} users and {PostCount} discussions.",
            users.Count,
            posts.Count);
    }

    private Dictionary<string, User> CreateUsers(DateTimeOffset now)
    {
        var users = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase);

        foreach (var username in MemberUsernames)
        {
            users[username] = CreateUser(username, UserRole.Member, now);
        }

        users[ModeratorUsername] = CreateUser(ModeratorUsername, UserRole.Moderator, now);

        return users;
    }

    private User CreateUser(string username, UserRole role, DateTimeOffset now)
    {
        var user = User.Register(username, $"{username}@forum.local", passwordHash: "placeholder", now, role);

        // Hashing needs the user the hash belongs to, so the real value is set once it exists.
        user.ChangePassword(passwordHasher.HashPassword(user, SeedPassword));

        // Seeded accounts are ready to sign in; nobody can click a link in a seeded mailbox.
        user.VerifyEmail(now);

        return user;
    }

    private static List<Post> CreatePosts(Dictionary<string, User> users, DateTimeOffset now)
    {
        var moderator = users[ModeratorUsername];
        var posts = new List<Post>(SeedContent.Posts.Length);

        foreach (var seed in SeedContent.Posts)
        {
            var author = users[seed.AuthorUsername];
            var createdAt = now.AddDays(-seed.DaysAgo);
            var post = Post.Create(author.Id, seed.Title, seed.Body, createdAt);

            AddLikes(post, users, author, seed.LikeCount, createdAt);
            AddReplies(post, users, author, seed.Replies, createdAt);

            if (seed.IsFlagged)
            {
                post.Flag(moderator, ModerationTag.MisleadingOrFalse, createdAt.AddHours(6));
            }

            posts.Add(post);
        }

        return posts;
    }

    /// <summary>
    /// Spreads likes across everyone except the author, so the self-like rule holds in seed data
    /// too. There are only four accounts, so a requested count above three is capped.
    /// </summary>
    private static void AddLikes(
        Post post,
        Dictionary<string, User> users,
        User author,
        int requestedCount,
        DateTimeOffset createdAt)
    {
        var candidates = users.Values
            .Where(user => user.Id != author.Id)
            .OrderBy(user => user.Username, StringComparer.Ordinal)
            .ToList();

        foreach (var user in candidates.Take(Math.Min(requestedCount, candidates.Count)))
        {
            post.Like(user.Id, createdAt.AddHours(1));
        }
    }

    private static void AddReplies(
        Post post,
        Dictionary<string, User> users,
        User author,
        IReadOnlyList<string> replies,
        DateTimeOffset createdAt)
    {
        var responders = users.Values
            .Where(user => user.Id != author.Id)
            .OrderBy(user => user.Username, StringComparer.Ordinal)
            .ToList();

        for (var index = 0; index < replies.Count; index++)
        {
            var responder = responders[index % responders.Count];
            post.Reply(responder.Id, replies[index], createdAt.AddHours(2 + index));
        }
    }
}
