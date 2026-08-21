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

        logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", traceId);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1",
                Title = "An unexpected error occurred.",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "The request could not be completed. Quote the trace identifier when reporting this.",
                Extensions = { ["traceId"] = traceId },
            },
        });
    }
}
