using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Forum.Api.Errors;

public static class ValidationProblems
{
    /// <summary>
    /// Makes a rejected request look like every other failure: a problem document, with the same
    /// media type and trace identifier. The framework default answers as plain JSON, which would
    /// leave callers parsing two shapes.
    /// </summary>
    public static IServiceCollection AddValidationProblemDetails(this IServiceCollection services)
    {
        // PostConfigure, because the framework sets this option after ordinary configuration runs.
        services.PostConfigure<ApiBehaviorOptions>(options =>
            options.InvalidModelStateResponseFactory = context =>
                new ValidationProblemResult(BuildProblemDetails(context)));

        return services;
    }

    private static ProblemDetails BuildProblemDetails(ActionContext context)
    {
        var traceId = context.HttpContext.Features.Get<IHttpActivityFeature>()?.Activity?.Id
            ?? context.HttpContext.TraceIdentifier;

        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Extensions =
            {
                // Carried as an extension rather than by using ValidationProblemDetails: the writer
                // serialises the declared type, so a subclass would lose these on the way out.
                ["errors"] = CollectErrors(context.ModelState),
                ["traceId"] = traceId,
            },
        };
    }

    private static Dictionary<string, string[]> CollectErrors(ModelStateDictionary modelState)
        => modelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

    /// <summary>
    /// Writes through the problem details service, the same path unhandled failures take, so the
    /// media type matches rather than depending on content negotiation.
    /// </summary>
    private sealed class ValidationProblemResult(ProblemDetails problemDetails) : IActionResult
    {
        public async Task ExecuteResultAsync(ActionContext context)
        {
            var problemDetailsService = context.HttpContext.RequestServices
                .GetRequiredService<IProblemDetailsService>();

            context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context.HttpContext,
                ProblemDetails = problemDetails,
            });
        }
    }
}
