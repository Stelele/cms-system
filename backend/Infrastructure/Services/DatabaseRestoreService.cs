using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class DatabaseRestoreService(
    ILogger<DatabaseRestoreService> logger,
    IConfiguration configuration) : IHostedService
{
    private readonly ILogger<DatabaseRestoreService> _logger = logger;
    private readonly string _databasePath = configuration["Database:Path"] ?? "cms.db";

    public static void EnsureDatabaseExists(IConfiguration configuration, ILogger<DatabaseRestoreService> logger)
    {
        var databasePath = configuration["Database:Path"] ?? "cms.db";
        if (File.Exists(databasePath))
        {
            logger.LogInformation("Database already exists at {Path}, skipping restore", databasePath);
            return;
        }

        logger.LogInformation("No database found at {Path}, will create new database", databasePath);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
