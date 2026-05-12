using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class FileCleanupService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<FileCleanupService> logger
) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(
        configuration.GetValue<int>("FileCleanup:CleanupIntervalMinutes", 60));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("File cleanup service started. Interval: {Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, stoppingToken);
                await CleanupOrphanedFilesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("File cleanup service stopping");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during file cleanup");
            }
        }
    }

    private async Task CleanupOrphanedFilesAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
        var r2 = scope.ServiceProvider.GetRequiredService<IR2StorageService>();

        var now = DateTimeOffset.UtcNow;
        var filesToDelete = await db.FileItems
            .Where(f => f.MarkedForDeletionAt != null && f.MarkedForDeletionAt <= now)
            .ToListAsync(ct);

        foreach (var file in filesToDelete)
        {
            try
            {
                await db.Entry(file).Collection(f => f.Posts).LoadAsync(ct);

                if (file.Posts.Count > 0)
                {
                    file.MarkedForDeletionAt = null;
                    continue;
                }

                await r2.DeleteAsync(r2.PublicBucket, file.StoragePath, ct);
                db.FileItems.Remove(file);

                logger.LogInformation("Deleted orphaned file {FileId} ({FileName}) from R2 and DB", file.Id, file.FileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete orphaned file {FileId}", file.Id);
            }
        }

        if (filesToDelete.Count > 0)
            await db.SaveChangesAsync(ct);
    }
}
