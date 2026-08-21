using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Forum.Infrastructure.Persistence;

public static class DatabaseStartup
{
    /// <summary>
    /// Brings the schema up to date and, when asked, fills an empty database with sample content.
    /// Running migrations at startup suits a single-instance deployment like this one; a fleet
    /// would apply them as a separate step instead.
    /// </summary>
    public static async Task InitialiseDatabaseAsync(this IHost host, bool seed)
    {
        using var scope = host.Services.CreateScope();

        var database = scope.ServiceProvider.GetRequiredService<ForumDbContext>();
        await database.Database.MigrateAsync();

        if (!seed)
        {
            return;
        }

        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        // Seeded accounts share a published password, so they must never reach a real deployment.
        if (!environment.IsDevelopment())
        {
            scope.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(typeof(DatabaseStartup))
                .LogWarning(
                    "Seeding was requested but refused: the {Environment} environment is not Development.",
                    environment.EnvironmentName);
            return;
        }

        await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync();
    }
}
