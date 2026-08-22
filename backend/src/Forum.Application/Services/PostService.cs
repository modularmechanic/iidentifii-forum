using Forum.Application.Common.Exceptions;
using Forum.Application.Common.Interfaces;
using Forum.Application.Common.Models;
using Forum.Application.Dtos;
using Forum.Application.Projections;
using Forum.Application.Queries;
using Forum.Domain.Posts;
using Microsoft.EntityFrameworkCore;

namespace Forum.Application.Services;

/// <summary>Reads and writes discussions. Rules that must always hold live on the entity.</summary>
public sealed class PostService(IForumDbContext database, TimeProvider clock)
{
    /// <summary>Returns one page of discussions, filtered and ordered as the query asks.</summary>
    public async Task<PagedResult<PostDto>> GetPageAsync(
        PostQuery query,
        Guid? currentUserId,
        CancellationToken cancellationToken)
    {
        var posts = Filter(database.Posts.AsNoTracking(), query);

        var totalCount = await posts.CountAsync(cancellationToken);

        var items = await Order(posts, query)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(PostProjections.ToDto(currentUserId))
            .ToListAsync(cancellationToken);

        return new PagedResult<PostDto>(items, query.Page, query.PageSize, totalCount);
    }

    /// <summary>Returns one discussion, or reports that it does not exist.</summary>
    public async Task<PostDto> GetByIdAsync(Guid id, Guid? currentUserId, CancellationToken cancellationToken)
    {
        var post = await database.Posts
            .AsNoTracking()
            .Where(candidate => candidate.Id == id)
            .Select(PostProjections.ToDto(currentUserId))
            .SingleOrDefaultAsync(cancellationToken);

        return post ?? throw NotFoundException.Discussion(id);
    }

    /// <summary>Starts a discussion and returns it as the reader will see it.</summary>
    public async Task<PostDto> CreateAsync(
        Guid authorId,
        CreatePostRequest request,
        CancellationToken cancellationToken)
    {
        var post = Post.Create(authorId, request.Title, request.Body, clock.GetUtcNow());

        database.Posts.Add(post);
        await database.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(post.Id, authorId, cancellationToken);
    }

    /// <summary>
    /// Records a like. The rules about liking your own discussion, or liking twice, live on the
    /// entity; two requests arriving together are caught by the unique index instead.
    /// </summary>
    public async Task LikeAsync(Guid postId, Guid userId, CancellationToken cancellationToken)
    {
        // Only this caller's like is loaded, not every like the discussion has. The entity asks
        // whether the collection already holds one for them, which is true of a filtered load as
        // much as a whole one — and a popular discussion no longer costs a row per admirer to
        // add one more.
        var post = await database.Posts
            .Include(candidate => candidate.Likes.Where(like => like.UserId == userId))
            .SingleOrDefaultAsync(candidate => candidate.Id == postId, cancellationToken)
            ?? throw NotFoundException.Discussion(postId);

        post.Like(userId, clock.GetUtcNow());

        await database.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Removes a like, or reports that there was none to remove.</summary>
    public async Task UnlikeAsync(Guid postId, Guid userId, CancellationToken cancellationToken)
    {
        // The same: removing one like does not require reading the rest.
        var post = await database.Posts
            .Include(candidate => candidate.Likes.Where(like => like.UserId == userId))
            .SingleOrDefaultAsync(candidate => candidate.Id == postId, cancellationToken)
            ?? throw NotFoundException.Discussion(postId);

        if (post.Unlike(userId) is null)
        {
            throw new NotFoundException("You have not liked this discussion.");
        }

        await database.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Rewrites a discussion. Only its author may.</summary>
    public async Task<PostDto> UpdateAsync(
        Guid postId,
        Guid actorId,
        CreatePostRequest request,
        CancellationToken cancellationToken)
    {
        var post = await FindAsync(postId, cancellationToken);

        post.Update(actorId, request.Title, request.Body, clock.GetUtcNow());

        await database.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(postId, actorId, cancellationToken);
    }

    /// <summary>
    /// Removes a discussion. Its replies, likes and flags go with it, which the database does by
    /// cascade rather than this method by hand.
    /// </summary>
    public async Task DeleteAsync(Guid postId, Guid actorId, CancellationToken cancellationToken)
    {
        var post = await FindAsync(postId, cancellationToken);

        post.EnsureOwnedBy(actorId);

        database.Posts.Remove(post);
        await database.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Marks a discussion as misleading or false. Whether the caller may is decided by the
    /// entity, which is given the member rather than a claim to inspect.
    /// </summary>
    public async Task FlagAsync(
        Guid postId,
        Guid moderatorId,
        ModerationTag tag,
        CancellationToken cancellationToken)
    {
        var post = await database.Posts
            .Include(candidate => candidate.Tags)
            .SingleOrDefaultAsync(candidate => candidate.Id == postId, cancellationToken)
            ?? throw NotFoundException.Discussion(postId);

        var moderator = await database.Users
            .SingleOrDefaultAsync(candidate => candidate.Id == moderatorId, cancellationToken)
            ?? throw new NotFoundException("The signed-in member no longer exists.");

        var postTag = post.Flag(moderator, tag, clock.GetUtcNow());

        // The flag carries its own identifier, so reaching the parent's collection is not enough
        // to mark it new.
        database.PostTags.Add(postTag);

        await database.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Takes a flag off, or reports that it was not there.</summary>
    public async Task UnflagAsync(
        Guid postId,
        Guid moderatorId,
        ModerationTag tag,
        CancellationToken cancellationToken)
    {
        var post = await database.Posts
            .Include(candidate => candidate.Tags)
            .SingleOrDefaultAsync(candidate => candidate.Id == postId, cancellationToken)
            ?? throw NotFoundException.Discussion(postId);

        var moderator = await database.Users
            .SingleOrDefaultAsync(candidate => candidate.Id == moderatorId, cancellationToken)
            ?? throw new NotFoundException("The signed-in member no longer exists.");

        if (post.Unflag(moderator, tag) is null)
        {
            throw new NotFoundException("This discussion does not carry that flag.");
        }

        await database.SaveChangesAsync(cancellationToken);
    }

    private async Task<Post> FindAsync(Guid postId, CancellationToken cancellationToken)
        => await database.Posts.SingleOrDefaultAsync(candidate => candidate.Id == postId, cancellationToken)
            ?? throw NotFoundException.Discussion(postId);

    private static IQueryable<Post> Filter(IQueryable<Post> posts, PostQuery query)
    {
        if (query.From is { } from)
        {
            var start = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            posts = posts.Where(post => post.CreatedAt >= start);
        }

        // The last representable day has no next day to bound against, and nothing can be later
        // than it, so the filter would exclude nothing anyway.
        if (query.To is { } to && to < DateOnly.MaxValue)
        {
            // Exclusive upper bound on the next day, so the whole of "to" is included.
            var endExclusive = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            posts = posts.Where(post => post.CreatedAt < endExclusive);
        }

        if (!string.IsNullOrWhiteSpace(query.Author))
        {
            // Usernames are stored as citext, so this compares without regard to case and still
            // uses the unique index.
            var author = query.Author.Trim();
            posts = posts.Where(post => post.Author.Username == author);
        }

        if (query.Tag is { } tag)
        {
            posts = posts.Where(post => post.Tags.Any(postTag => postTag.Tag == tag));
        }

        return posts;
    }

    /// <summary>
    /// Orders the page. A second and third key make the order total: without them, discussions
    /// sharing a like count or a timestamp could appear on two pages, or on none.
    /// </summary>
    private static IQueryable<Post> Order(IQueryable<Post> posts, PostQuery query)
    {
        var ascending = query.Order == SortOrder.Ascending;

        return query.Sort switch
        {
            PostSort.LikeCount => ascending
                ? posts.OrderBy(post => post.Likes.Count).ThenBy(post => post.CreatedAt).ThenBy(post => post.Id)
                : posts.OrderByDescending(post => post.Likes.Count)
                    .ThenByDescending(post => post.CreatedAt)
                    .ThenBy(post => post.Id),
            _ => ascending
                ? posts.OrderBy(post => post.CreatedAt).ThenBy(post => post.Id)
                : posts.OrderByDescending(post => post.CreatedAt).ThenBy(post => post.Id),
        };
    }
}
