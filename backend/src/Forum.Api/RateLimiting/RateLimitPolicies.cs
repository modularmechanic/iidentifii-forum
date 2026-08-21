using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Threading.RateLimiting;

namespace Forum.Api.RateLimiting;

/// <summary>Request limits protecting the API, and the authentication routes in particular.</summary>
public static class RateLimitPolicies
{
    /// <summary>Applied to sign-in, registration and anything else that sends email.</summary>
    public const string Authentication = "authentication";

    /// <summary>
    /// Applied to reading the current session. It sits on the authentication controller but is
    /// not an attempt to authenticate, so spending the small sign-in budget on it would sign a
    /// reader out of a page they were only looking at. It gets the ordinary allowance rather than
    /// being excused altogether: turning the limiter off takes the global one with it and leaves
    /// this endpoint, which does reach the database, as the only unmetered route in the API.
    /// </summary>
    public const string Session = "session";

    public static IServiceCollection AddForumRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<RateLimitOptions>()
            .Bind(configuration.GetSection(RateLimitOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var limits = configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>()
            ?? new RateLimitOptions();

        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetClientKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limits.GlobalPermitsPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                    }));

            options.AddPolicy(Authentication, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetClientKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limits.AuthenticationPermitsPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                    }));

            options.AddPolicy(Session, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetClientKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limits.GlobalPermitsPerMinute,
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
