using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class DatabaseRestoreService(
    ILogger<DatabaseRestoreService> logger,
    GoogleDriveService googleDrive,
    IConfiguration configuration) : IHostedService
{
    private readonly ILogger<DatabaseRestoreService> _logger = logger;
    private readonly GoogleDriveService _googleDrive = googleDrive;
    private readonly int _maxRetries = configuration.GetValue<int?>("GoogleDrive:RestoreMaxRetries") ?? 3;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(_googleDrive.DatabasePath))
        {
            _logger.LogInformation("Database already exists at {Path}, skipping restore", _googleDrive.DatabasePath);
            return Task.CompletedTask;
        }

        _logger.LogInformation("No database found at {Path}, attempting restore from Google Drive", _googleDrive.DatabasePath);
        return RestoreDatabaseAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task RestoreDatabaseAsync(CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(_googleDrive.DatabasePath);

        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Attempting to restore database (attempt {Attempt}/{MaxRetries})", attempt, _maxRetries);

                var existingFile = await _googleDrive.FindFileAsync(fileName, cancellationToken);

                if (existingFile is null)
                {
                    _logger.LogWarning("No backup found in Google Drive, creating new database");
                    return;
                }

                using var memoryStream = new MemoryStream();
                await _googleDrive.DownloadFileAsync(existingFile.Id, memoryStream, cancellationToken);

                var directory = Path.GetDirectoryName(_googleDrive.DatabasePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllBytesAsync(_googleDrive.DatabasePath, memoryStream.ToArray(), cancellationToken);

                _logger.LogInformation("Database restored successfully from Google Drive");
                return;
            }
            catch (Exception ex) when (attempt < _maxRetries)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                _logger.LogWarning(ex, "Database restore attempt {Attempt} failed, retrying in {Delay}s", attempt, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database restore failed after {MaxRetries} attempts", _maxRetries);
                throw new InvalidOperationException($"Failed to restore database after {_maxRetries} attempts", ex);
            }
        }
    }
}