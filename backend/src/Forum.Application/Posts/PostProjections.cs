using System.Linq.Expressions;
using Forum.Domain.Posts;

namespace Forum.Application.Posts;

/// <summary>
/// The single definition of how a discussion becomes a payload. Both the list and the detail
/// view use it, so their shapes cannot drift, and it translates to one query with no per-row
/// follow-ups.
/// </summary>
public static class PostProjections
{
    public static Expression<Func<Post, PostDto>> ToDto(Guid? currentUserId) =>
        post => new PostDto(
            post.Id,
            post.Title,
            post.Body,
            new AuthorDto(post.Author.Id, post.Author.Username),
            post.CreatedAt,
            post.UpdatedAt,
            post.Likes.Count,
            post.Comments.Count,
            post.Tags
                .OrderBy(tag => tag.CreatedAt)
                .Select(tag => new ModerationTagDto(
                    tag.Tag,
                    tag.TaggedByUser.Username,
                    tag.CreatedAt))
                .ToList(),
            currentUserId != null && post.Likes.Any(like => like.UserId == currentUserId));
}
