using Forum.Api.Auth;
using Forum.Application.Dtos;
using Forum.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forum.Api.Controllers;

/// <summary>
/// Replies, addressed on their own. Reading them belongs to the discussion that holds them, so
/// only changing one lives here.
/// </summary>
[ApiController]
[Route("api/v1/comments")]
[Produces("application/json")]
public sealed class CommentsController(CommentService comments) : ControllerBase
{
    /// <summary>Rewrites a reply. Only its author may.</summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType<CommentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentDto>> Update(
        Guid id,
        CreateCommentRequest request,
        CancellationToken cancellationToken)
        => Ok(await comments.UpdateAsync(id, User.GetRequiredUserId(), request, cancellationToken));

    /// <summary>Removes a reply. Only its author may.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await comments.DeleteAsync(id, User.GetRequiredUserId(), cancellationToken);

        return NoContent();
    }
}
