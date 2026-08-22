using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;

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
            .GroupBy(entry => NameField(entry.Key))
            .ToDictionary(
                group => group.Key,
                group => group
                    .SelectMany(entry => entry.Value!.Errors.Select(error => Describe(entry.Key, error)))
                    .Distinct()
                    .ToArray());

    /// <summary>
    /// The field a rejected value belongs to. A body the reader could not parse is reported
    /// against its JSON path, so the leading <c>$.</c> is dropped and the caller is told which
    /// field it sent rather than where in the document the parser gave up.
    /// </summary>
    private static string NameField(string key)
        => IsParseFailure(key) ? key[(key.LastIndexOf('.') + 1)..] : key;

    /// <summary>
    /// The message a caller sees. The parser explains a value it could not read in its own terms,
    /// naming the CLR type it was aiming at and the byte it stopped on. Neither means anything to
    /// whoever sent the request, and both describe the inside of the API, so they are replaced.
    /// Everything the API checks itself already has a message written for a reader, and keeps it.
    /// </summary>
    private static string Describe(string key, ModelError error)
        => IsParseFailure(key) || error.ErrorMessage.Length == 0
            ? "The value is not in a form this field accepts."
            : error.ErrorMessage;

    /// <summary>
    /// True when the key names a place in the JSON rather than a field of the request. A failure
    /// against a property is keyed "$.name"; one against the document itself is keyed "$" or
    /// nothing at all, and its message is the parser's, which is not for the caller to read.
    /// </summary>
    private static bool IsParseFailure(string key)
        => key.Length == 0
            || key.Equals("$", StringComparison.Ordinal)
            || key.StartsWith("$.", StringComparison.Ordinal)
            || key.StartsWith("$[", StringComparison.Ordinal);

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
