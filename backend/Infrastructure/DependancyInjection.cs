using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure;

public static class DependancyInjection
{
    public static WebApplication MapInfrastructure(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
        var canConnect = db.Database.CanConnect();
        app.Logger.LogInformation("Can connect to database: {CanConnect}", canConnect);

        try
        {
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "An error occurred while applying database migrations.");
            throw;
        }

        return app;
    }

    public static WebApplicationBuilder AddInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IR2StorageService, R2StorageService>();

        var r2 = builder.Services.BuildServiceProvider()
            .GetRequiredService<IR2StorageService>();
        var dbRestoreLogger = builder.Services.BuildServiceProvider()
            .GetRequiredService<ILogger<DatabaseRestoreService>>();
        DatabaseRestoreService.EnsureDatabaseExists(r2, builder.Configuration, dbRestoreLogger);

        builder.Services.AddDbContext<CmsDbContext>(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString("Sqlite") ??
                "Data Source=cms.db";
            options.UseSqlite(connectionString);
        });

        builder.Services.AddHttpClient<IGroqService, GroqService>();

        builder.Services.AddSingleton<DatabaseSyncService>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<DatabaseSyncService>());

        builder.Configuration["ContentRootPath"] = builder.Environment.ContentRootPath;

        return builder;
    }
}
