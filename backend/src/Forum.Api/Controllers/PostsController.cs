using Forum.Application.Common.Models;
using Forum.Application.Dtos;
using Forum.Application.Queries;
using Forum.Domain.Posts;
using Forum.Api.Auth;
using Forum.Application.Services;
using Microsoft.AspNetCore.Authorization;
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
        => Ok(await posts.GetPageAsync(query, User.GetUserId(), cancellationToken));

    /// <summary>Returns one discussion.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<PostDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await posts.GetByIdAsync(id, User.GetUserId(), cancellationToken));

    /// <summary>Returns a page of replies to one discussion, oldest first.</summary>
    [HttpGet("{id:guid}/comments")]
    [ProducesResponseType<PagedResult<CommentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<CommentDto>>> GetComments(
        Guid id,
        [FromQuery] CommentQuery query,
        CancellationToken cancellationToken)
        => Ok(await comments.GetPageAsync(id, query, cancellationToken));

    /// <summary>Starts a discussion.</summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType<PostDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PostDto>> Create(
        CreatePostRequest request,
        CancellationToken cancellationToken)
    {
        var post = await posts.CreateAsync(User.GetRequiredUserId(), request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = post.Id }, post);
    }

    /// <summary>Replies to a discussion.</summary>
    [HttpPost("{id:guid}/comments")]
    [Authorize]
    [ProducesResponseType<CommentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentDto>> Reply(
        Guid id,
        CreateCommentRequest request,
        CancellationToken cancellationToken)
    {
        var comment = await comments.CreateAsync(
            id,
            User.GetRequiredUserId(),
            request,
            cancellationToken);

        return CreatedAtAction(nameof(GetComments), new { id }, comment);
    }

    /// <summary>Likes a discussion. Nobody likes their own, and nobody likes one twice.</summary>
    [HttpPost("{id:guid}/like")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Like(Guid id, CancellationToken cancellationToken)
    {
        await posts.LikeAsync(id, User.GetRequiredUserId(), cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id }, value: null);
    }

    /// <summary>Takes a like back.</summary>
    [HttpDelete("{id:guid}/like")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unlike(Guid id, CancellationToken cancellationToken)
    {
        await posts.UnlikeAsync(id, User.GetRequiredUserId(), cancellationToken);

        return NoContent();
    }

    /// <summary>Rewrites a discussion. Only its author may.</summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType<PostDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostDto>> Update(
        Guid id,
        CreatePostRequest request,
        CancellationToken cancellationToken)
        => Ok(await posts.UpdateAsync(id, User.GetRequiredUserId(), request, cancellationToken));

    /// <summary>Removes a discussion, and with it every reply, like and flag it carried.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await posts.DeleteAsync(id, User.GetRequiredUserId(), cancellationToken);

        return NoContent();
    }

    /// <summary>Marks a discussion as misleading or false.</summary>
    [HttpPost("{id:guid}/tags")]
    [Authorize(Policy = AuthenticationSetup.ModeratorPolicy)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Flag(
        Guid id,
        FlagPostRequest request,
        CancellationToken cancellationToken)
    {
        await posts.FlagAsync(id, User.GetRequiredUserId(), request.Tag, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id }, value: null);
    }

    /// <summary>Takes a flag off a discussion.</summary>
    [HttpDelete("{id:guid}/tags/{tag}")]
    [Authorize(Policy = AuthenticationSetup.ModeratorPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unflag(
        Guid id,
        ModerationTag tag,
        CancellationToken cancellationToken)
    {
        await posts.UnflagAsync(id, tag, cancellationToken);

        return NoContent();
    }
}
