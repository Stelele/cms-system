using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class DatabaseSyncService : BackgroundService, IDatabaseSyncService
{
    private readonly ILogger<DatabaseSyncService> _logger;

    public DatabaseSyncService(
        ILogger<DatabaseSyncService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Database sync service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
                await SyncDatabaseAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Database sync service stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database sync");
            }
        }
    }

    public Task SyncDatabaseAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Database sync: backup not yet configured");
        return Task.CompletedTask;
    }
}
