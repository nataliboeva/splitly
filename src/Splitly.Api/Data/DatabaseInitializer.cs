using Microsoft.EntityFrameworkCore;

namespace Splitly.Api.Data;

public static class DatabaseInitializer
{
    /// <summary>
    /// Applies pending migrations on startup. Only used in Development; a real deployment would run
    /// migrations as a separate pipeline step instead of racing several app instances.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();
    }
}
