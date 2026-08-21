using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Forum.Api.RateLimiting;

/// <summary>Request limits protecting the API, and the authentication routes in particular.</summary>
public static class RateLimitPolicies
{
    /// <summary>Applied to sign-in, registration and anything else that sends email.</summary>
    public const string Authentication = "authentication";

    private const int GlobalPermitsPerMinute = 120;
    private const int AuthenticationPermitsPerMinute = 10;

    public static IServiceCollection AddForumRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetClientKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = GlobalPermitsPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                    }));

            options.AddPolicy(Authentication, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetClientKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = AuthenticationPermitsPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                    }));

            options.OnRejected = WriteProblemDetailsAsync;
        });

        return services;
    }

    /// <summary>Partitions by caller address, falling back to a shared bucket when it is unknown.</summary>
    private static string GetClientKey(HttpContext context)
        => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static async ValueTask WriteProblemDetailsAsync(
        OnRejectedContext context,
        CancellationToken cancellationToken)
    {
        var retryAfterSeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
            ? (int)retryAfter.TotalSeconds
            : 60;

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.Headers.RetryAfter =
            retryAfterSeconds.ToString(CultureInfo.InvariantCulture);

        var problemDetailsService = context.HttpContext.RequestServices
            .GetRequiredService<IProblemDetailsService>();

        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context.HttpContext,
            ProblemDetails = new ProblemDetails
            {
                Type = "https://datatracker.ietf.org/doc/html/rfc6585#section-4",
                Title = "Too many requests.",
                Status = StatusCodes.Status429TooManyRequests,
                Detail = $"Rate limit exceeded. Try again in {retryAfterSeconds} seconds.",
            },
        });
    }
}
