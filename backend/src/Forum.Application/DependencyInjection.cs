using Forum.Application.Comments;
using Forum.Application.Posts;
using Microsoft.Extensions.DependencyInjection;

namespace Forum.Application;

public static class DependencyInjection
{
    /// <summary>Registers the services that carry the forum's behaviour.</summary>
    public static IServiceCollection AddForumApplication(this IServiceCollection services)
    {
        services.AddScoped<PostService>();
        services.AddScoped<CommentService>();

        return services;
    }
}
