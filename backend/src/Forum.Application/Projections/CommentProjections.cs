using Forum.Application.Dtos;
using Forum.Domain.Comments;
using System.Linq.Expressions;

namespace Forum.Application.Projections;

/// <summary>
/// The single definition of how a reply becomes a payload, so the list and a freshly written
/// reply cannot describe the same thing differently.
/// </summary>
public static class CommentProjections
{
    public static Expression<Func<Comment, CommentDto>> ToDto() =>
        comment => new CommentDto(
            comment.Id,
            comment.PostId,
            comment.Body,
            new AuthorDto(comment.Author.Id, comment.Author.Username),
            comment.CreatedAt,
            comment.UpdatedAt);
}
