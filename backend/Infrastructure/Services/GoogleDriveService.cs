using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

public class GoogleDriveService
{
    private readonly IConfiguration _configuration;
    private readonly string _databasePath;
    private readonly string? _folderId;
    private readonly string? _clientId;
    private readonly string? _clientSecret;
    private readonly string? _refreshToken;

    public string DatabasePath => _databasePath;
    public string? FolderId => _folderId;

    public GoogleDriveService(IConfiguration configuration)
    {
        _configuration = configuration;
        var connectionString = _configuration.GetValue<string>("ConnectionStrings:Sqlite") ?? "Data Source=cms.db";
        _databasePath = connectionString.Replace("Data Source=", "");
        _folderId = _configuration["GoogleDrive:FolderId"];
        _clientId = _configuration["GoogleDrive:ClientID"];
        _clientSecret = _configuration["GoogleDrive:ClientSecret"];
        _refreshToken = _configuration["GoogleDrive:RefreshToken"];
    }

    public DriveService CreateDriveService(string applicationName)
    {
        var clientId = _clientId ?? throw new ArgumentNullException("ClientId is missing");
        var clientSecret = _clientSecret ?? throw new ArgumentNullException("ClientSecret is missing");
        var refreshToken = _refreshToken ?? throw new ArgumentNullException("RefreshToken is missing");

        var credential = new UserCredential(
            new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret }
            }),
            "user",
            new TokenResponse { RefreshToken = refreshToken });

        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = applicationName
        });
    }

    public async Task<Google.Apis.Drive.v3.Data.File?> FindFileAsync(string fileName, CancellationToken ct)
    {
        var driveService = CreateDriveService("CMS");
        var listRequest = driveService.Files.List();
        listRequest.Q = $"name = '{fileName}' and '{_folderId}' in parents and trashed = false";
        listRequest.Spaces = "drive";
        listRequest.Fields = "files(id, name)";

        var result = await listRequest.ExecuteAsync(ct);
        return result.Files?.FirstOrDefault();
    }

    public async Task DownloadFileAsync(string fileId, Stream destination, CancellationToken ct)
    {
        var driveService = CreateDriveService("CMS");
        var request = driveService.Files.Get(fileId);
        await request.DownloadAsync(destination, ct);
    }

    public async Task UploadOrUpdateFileAsync(Stream content, CancellationToken ct)
    {
        var driveService = CreateDriveService("CMS");
        var fileName = Path.GetFileName(_databasePath);

        var existingFile = await FindFileAsync(fileName, ct);

        using var tempStream = new MemoryStream();
        content.Position = 0;
        await content.CopyToAsync(tempStream);
        tempStream.Position = 0;

        if (existingFile is not null)
        {
            var updateRequest = driveService.Files.Update(
                new Google.Apis.Drive.v3.Data.File { Name = fileName },
                existingFile.Id,
                tempStream,
                "application/vnd.sqlite3");
            updateRequest.Fields = "id";
            await updateRequest.UploadAsync(ct);
        }
        else
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File { Name = fileName, Parents = [_folderId] };
            var createRequest = driveService.Files.Create(fileMetadata, tempStream, "application/vnd.sqlite3");
            createRequest.Fields = "id";
            await createRequest.UploadAsync(ct);
        }
    }
}