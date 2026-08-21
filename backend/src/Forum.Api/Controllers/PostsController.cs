using Forum.Application.Comments;
using Forum.Application.Common.Models;
using Forum.Application.Posts;
using Microsoft.AspNetCore.Mvc;

namespace Forum.Api.Controllers;

/// <summary>Discussions: the forum's main content.</summary>
[ApiController]
[Route("api/v1/posts")]
[Produces("application/json")]
public sealed class PostsController(PostService posts, CommentService comments) : ControllerBase
{
    /// <summary>Returns a page of discussions, newest first.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<PostDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<PostDto>>> GetPage(
        [FromQuery] PostQuery query,
        CancellationToken cancellationToken)
        => Ok(await posts.GetPageAsync(query, currentUserId: null, cancellationToken));

    /// <summary>Returns one discussion.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<PostDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await posts.GetByIdAsync(id, currentUserId: null, cancellationToken));

    /// <summary>Returns a page of replies to one discussion, oldest first.</summary>
    [HttpGet("{id:guid}/comments")]
    [ProducesResponseType<PagedResult<CommentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<CommentDto>>> GetComments(
        Guid id,
        [FromQuery] CommentQuery query,
        CancellationToken cancellationToken)
        => Ok(await comments.GetPageAsync(id, query, cancellationToken));
}
