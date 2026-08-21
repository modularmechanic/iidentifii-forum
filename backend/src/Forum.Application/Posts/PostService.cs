using Forum.Application.Common.Exceptions;
using Forum.Application.Common.Interfaces;
using Forum.Application.Common.Models;
using Forum.Domain.Posts;
using Microsoft.EntityFrameworkCore;

namespace Forum.Application.Posts;

/// <summary>Reads and writes discussions. Rules that must always hold live on the entity.</summary>
public sealed class PostService(IForumDbContext database)
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
