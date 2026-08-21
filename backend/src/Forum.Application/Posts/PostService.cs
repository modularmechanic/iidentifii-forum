using Forum.Application.Common.Exceptions;
using Forum.Application.Common.Interfaces;
using Forum.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Forum.Application.Posts;

/// <summary>Reads and writes discussions. Rules that must always hold live on the entity.</summary>
public sealed class PostService(IForumDbContext database)
{
    /// <summary>Returns one page of discussions, newest first.</summary>
    public async Task<PagedResult<PostDto>> GetPageAsync(
        PostQuery query,
        Guid? currentUserId,
        CancellationToken cancellationToken)
    {
        var posts = database.Posts.AsNoTracking();

        var totalCount = await posts.CountAsync(cancellationToken);

        var items = await posts
            .OrderByDescending(post => post.CreatedAt)
            .ThenByDescending(post => post.Id)
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
}
