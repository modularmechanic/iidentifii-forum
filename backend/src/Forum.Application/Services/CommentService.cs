using Forum.Application.Common.Exceptions;
using Forum.Application.Common.Interfaces;
using Forum.Application.Common.Models;
using Forum.Application.Dtos;
using Forum.Application.Projections;
using Forum.Application.Queries;
using Microsoft.EntityFrameworkCore;

namespace Forum.Application.Services;

/// <summary>Reads and writes replies to a discussion.</summary>
public sealed class CommentService(IForumDbContext database, TimeProvider clock)
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

        var ordered = query.Order == SortOrder.Descending
            ? comments.OrderByDescending(comment => comment.CreatedAt).ThenBy(comment => comment.Id)
            : comments.OrderBy(comment => comment.CreatedAt).ThenBy(comment => comment.Id);

        var items = await ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(CommentProjections.ToDto())
            .ToListAsync(cancellationToken);

        return new PagedResult<CommentDto>(items, query.Page, query.PageSize, totalCount);
    }

    /// <summary>Adds a reply to a discussion and returns it as the reader will see it.</summary>
    public async Task<CommentDto> CreateAsync(
        Guid postId,
        Guid authorId,
        CreateCommentRequest request,
        CancellationToken cancellationToken)
    {
        var post = await database.Posts
            .SingleOrDefaultAsync(candidate => candidate.Id == postId, cancellationToken)
            ?? throw NotFoundException.Discussion(postId);

        var comment = post.Reply(authorId, request.Body, clock.GetUtcNow());

        // The entity carries its own identifier, so reaching the parent's collection is not enough
        // to mark it new: without this it is taken for a row that already exists and updated.
        database.Comments.Add(comment);

        await database.SaveChangesAsync(cancellationToken);

        return await database.Comments
            .AsNoTracking()
            .Where(candidate => candidate.Id == comment.Id)
            .Select(CommentProjections.ToDto())
            .SingleAsync(cancellationToken);
    }
}
