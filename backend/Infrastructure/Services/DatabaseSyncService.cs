using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class DatabaseSyncService : BackgroundService, IDatabaseSyncService
{
    private readonly ILogger<DatabaseSyncService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _databasePath;
    private readonly string? _serviceAccountJson;
    private readonly string? _folderId;
    private readonly TimeSpan _interval;

    public DatabaseSyncService(ILogger<DatabaseSyncService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
var connectionString = _configuration.GetValue<string>("ConnectionStrings:Sqlite") ?? "Data Source=cms.db";
        _databasePath = connectionString.Replace("Data Source=", "");
        _serviceAccountJson = _configuration["GoogleDrive:ServiceAccountJson"];
        _folderId = _configuration["GoogleDrive:FolderId"];

        var intervalMinutes = _configuration.GetValue<int?>("GoogleDrive:SyncIntervalMinutes") ?? 15;
        _interval = TimeSpan.FromMinutes(intervalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Database sync service started. Sync interval: {Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, stoppingToken);
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

    public async Task SyncDatabaseAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_databasePath))
        {
            _logger.LogWarning("Database file not found at {Path}", _databasePath);
            return;
        }

        var tempDbPath = Path.GetTempFileName();
        try
        {
            _logger.LogInformation("Starting database sync to Google Drive");

            var driveService = CreateDriveService();
            var fileName = Path.GetFileName(_databasePath);

            var listRequest = driveService.Files.List();
            listRequest.Q = $"name = '{fileName}' and '{_folderId}' in parents and trashed = false";
            listRequest.Spaces = "drive";
            listRequest.Fields = "files(id, name)";

            var existingFiles = await listRequest.ExecuteAsync(cancellationToken);
            var existingFile = existingFiles.Files?.FirstOrDefault();

            using (var sourceDb = new SqliteConnection($"Data Source={_databasePath}"))
            using (var destinationDb = new SqliteConnection($"Data Source={tempDbPath};Pooling=False;"))
            {
                sourceDb.Open();
                destinationDb.Open();

                // This safely creates a consistent snapshot without locking the main app
                sourceDb.BackupDatabase(destinationDb);
            }

            using (var fileStream = new FileStream(tempDbPath, FileMode.Open, FileAccess.Read))
            {
                if (existingFile != null)
                {
                    var updateRequest = driveService.Files.Update(
                        new Google.Apis.Drive.v3.Data.File { Name = fileName },
                        existingFile.Id,
                        fileStream,
                        "application/vnd.sqlite3"
                    );
                    updateRequest.Fields = "id";

                    var progress = await updateRequest.UploadAsync(cancellationToken);

                    if (progress.Status == Google.Apis.Upload.UploadStatus.Completed)
                    {
                        _logger.LogInformation("Updated existing file in Google Drive: {FileId}", updateRequest.ResponseBody.Id);
                    }
                    else
                    {
                        _logger.LogError("Update failed: {Error}", progress.Exception?.Message);
                    }
                }
                else
                {
                    var fileMetadata = new Google.Apis.Drive.v3.Data.File
                    {
                        Name = fileName,
                        Parents = [_folderId]
                    };
                    var createRequest = driveService.Files.Create(fileMetadata, fileStream, "application/vnd.sqlite3");
                    createRequest.Fields = "id";

                    var progress = await createRequest.UploadAsync(cancellationToken);

                    if (progress.Status == Google.Apis.Upload.UploadStatus.Completed)
                    {
                        _logger.LogInformation("Created new file in Google Drive: {FileId}", createRequest.ResponseBody.Id);
                    }
                    else
                    {
                        _logger.LogError("Upload failed: {Error}", progress.Exception?.Message);
                    }
                }
            }

            _logger.LogInformation("Database sync completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync database to Google Drive.");
            throw;
        }
        finally
        {
            if (File.Exists(tempDbPath))
            {
                File.Delete(tempDbPath);
            }
        }
    }

    private DriveService CreateDriveService()
    {
        var clientId = _configuration["GoogleDrive:ClientID"]
        ?? throw new ArgumentNullException("ClientId is missing");
        var clientSecret = _configuration["GoogleDrive:ClientSecret"]
            ?? throw new ArgumentNullException("ClientSecret is missing");
        var refreshToken = _configuration["GoogleDrive:RefreshToken"]
            ?? throw new ArgumentNullException("RefreshToken is missing");

        var credential = new UserCredential(
            new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = clientId,
                        ClientSecret = clientSecret
                    }
                }),
            "user",
            new TokenResponse
            {
                RefreshToken = refreshToken
            });

        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "CMS Database Sync Service"
        });
    }
}