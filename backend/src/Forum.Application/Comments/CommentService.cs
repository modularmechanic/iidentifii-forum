using Forum.Application.Common.Exceptions;
using Forum.Application.Common.Interfaces;
using Forum.Application.Common.Models;
using Forum.Application.Posts;
using Microsoft.EntityFrameworkCore;

namespace Forum.Application.Comments;

/// <summary>Reads and writes replies to a discussion.</summary>
public sealed class CommentService(IForumDbContext database)
{
    /// <summary>Returns one page of replies, oldest first so a conversation reads in order.</summary>
    public async Task<PagedResult<CommentDto>> GetPageAsync(
        Guid postId,
        CommentQuery query,
        CancellationToken cancellationToken)
    {
        var postExists = await database.Posts
            .AsNoTracking()
            .AnyAsync(post => post.Id == postId, cancellationToken);

        if (!postExists)
        {
            throw NotFoundException.Discussion(postId);
        }

        var comments = database.Comments
            .AsNoTracking()
            .Where(comment => comment.PostId == postId);

        var totalCount = await comments.CountAsync(cancellationToken);

        var items = await comments
            .OrderBy(comment => comment.CreatedAt)
            .ThenBy(comment => comment.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(comment => new CommentDto(
                comment.Id,
                comment.PostId,
                comment.Body,
                new AuthorDto(comment.Author.Id, comment.Author.Username),
                comment.CreatedAt,
                comment.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<CommentDto>(items, query.Page, query.PageSize, totalCount);
    }
}
