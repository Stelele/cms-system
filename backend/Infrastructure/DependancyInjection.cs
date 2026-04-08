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
        builder.Services.AddSingleton<GoogleDriveService>();

        var googleDrive = new GoogleDriveService(builder.Configuration);
        var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<DatabaseRestoreService>>();
        DatabaseRestoreService.EnsureDatabaseExists(googleDrive, builder.Configuration, logger);

        builder.Services.AddDbContext<CmsDbContext>(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString("Sqlite") ??
                "Data Source=cms.db";
            options.UseSqlite(connectionString);
        });

        builder.Services.AddHttpClient<IGroqService, GroqService>();

        builder.Services.AddSingleton<IDatabaseSyncService, DatabaseSyncService>();
        builder.Services.AddHostedService(sp => (DatabaseSyncService)sp.GetRequiredService<IDatabaseSyncService>());

        builder.Configuration["ContentRootPath"] = builder.Environment.ContentRootPath;

        return builder;
    }
}
