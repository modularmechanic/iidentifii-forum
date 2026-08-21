using Forum.Application.Common.Exceptions;
using Forum.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace Forum.Api.Errors;

/// <summary>
/// Translates unhandled exceptions into RFC 7807 responses. Details of unexpected failures stay
/// in the logs; the client receives a trace identifier it can quote instead.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = httpContext.Features.Get<IHttpActivityFeature>()?.Activity?.Id
            ?? httpContext.TraceIdentifier;

        var (status, title, detail) = Describe(exception);

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", traceId);
        }
        else
        {
            logger.LogInformation(
                "Request refused with {Status}: {Message}. TraceId: {TraceId}",
                status,
                exception.Message,
                traceId);
        }

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Title = title,
                Status = status,
                Detail = detail,
                Extensions = { ["traceId"] = traceId },
            },
        });
    }

    /// <summary>
    /// Maps a failure onto a response. Anything unrecognised is reported without its message,
    /// so internal detail never reaches the caller.
    /// </summary>
    private static (int Status, string Title, string Detail) Describe(Exception exception) => exception switch
    {
        NotFoundException => (StatusCodes.Status404NotFound, "Not found.", exception.Message),
        ConflictException => (StatusCodes.Status409Conflict, "Already exists.", exception.Message),
        DomainException { Error: DomainError.Forbidden } =>
            (StatusCodes.Status403Forbidden, "Not allowed.", exception.Message),
        DomainException { Error: DomainError.Conflict } =>
            (StatusCodes.Status409Conflict, "Already done.", exception.Message),
        DomainException => (StatusCodes.Status422UnprocessableEntity, "Rule violated.", exception.Message),
        _ => (
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred.",
            "The request could not be completed. Quote the trace identifier when reporting this."),
    };
}
