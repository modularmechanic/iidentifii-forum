using Forum.Application.Common.Exceptions;
using Forum.Application.Common.Interfaces;
using Forum.Domain.Comments;
using Forum.Domain.Posts;
using Forum.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Forum.Infrastructure.Persistence;

public sealed class ForumDbContext(DbContextOptions<ForumDbContext> options)
    : DbContext(options), IForumDbContext
{
    private const string UniqueViolation = "23505";

    public DbSet<User> Users => Set<User>();

    public DbSet<UserToken> UserTokens => Set<UserToken>();

    public DbSet<Post> Posts => Set<Post>();

    public DbSet<PostLike> PostLikes => Set<PostLike>();

    public DbSet<PostTag> PostTags => Set<PostTag>();

    public DbSet<Comment> Comments => Set<Comment>();

    /// <summary>
    /// Saves changes, translating a unique-index violation into a conflict. Uniqueness is decided
    /// by the database rather than by reading first, which would still race under concurrency.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: UniqueViolation })
        {
            throw new ConflictException("That item already exists.", exception);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ForumDbContext).Assembly);
    }
}
