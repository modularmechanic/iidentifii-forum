using Forum.Domain.Comments;
using Forum.Domain.Posts;
using Forum.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Forum.Application.Common.Interfaces;

/// <summary>
/// The persistence seam. Services depend on this rather than the concrete context, which keeps
/// them testable without hiding Entity Framework behind a repository that would only repeat it.
/// </summary>
public interface IForumDbContext
{
    DbSet<User> Users { get; }

    DbSet<UserToken> UserTokens { get; }

    DbSet<Post> Posts { get; }

    DbSet<PostLike> PostLikes { get; }

    DbSet<PostTag> PostTags { get; }

    DbSet<Comment> Comments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
