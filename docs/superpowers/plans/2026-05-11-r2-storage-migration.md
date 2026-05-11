# R2 Storage Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace Google Drive (database backup) and local filesystem (media uploads) with Cloudflare R2.

**Architecture:** Single `R2StorageService` wraps `AmazonS3Client` and handles both the public bucket (CMS media) and private bucket (database backups). File upload handlers use it instead of local disk. Database sync/restore services use it instead of Google Drive API.

**Tech Stack:** `AWSSDK.S3` (R2 is S3-compatible), `AmazonS3Client` configured for R2 endpoint.

---

### Task 1: Add NuGet package and create R2StorageService

**Files:**
- Modify: `backend/Infrastructure/Infrastructure.csproj` — replace Google NuGets with `AWSSDK.S3`
- Create: `backend/Infrastructure/Services/R2StorageService.cs`
- Create: `backend/Infrastructure/Services/IR2StorageService.cs`

- [ ] **Step 1: Update Infrastructure.csproj**

Replace the `Google.Apis.Drive.v3` and `Google.Apis.Auth` packages with `AWSSDK.S3`:

```xml
<ItemGroup>
  <PackageReference Remove="Google.Apis.Drive.v3" />
  <PackageReference Remove="Google.Apis.Auth" />
</ItemGroup>
<ItemGroup>
  <PackageReference Include="AWSSDK.S3" Version="3.7.415.14" />
</ItemGroup>
```

- [ ] **Step 2: Restore packages and build**

```bash
dotnet restore
dotnet build
```

Expected: No errors.

- [ ] **Step 3: Create IR2StorageService interface**

Create `backend/Infrastructure/Services/IR2StorageService.cs`:

```csharp
namespace Infrastructure.Services;

public interface IR2StorageService
{
    Task UploadAsync(string bucket, string key, Stream content, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string bucket, string key, CancellationToken ct = default);
    Task DeleteAsync(string bucket, string key, CancellationToken ct = default);
    bool ObjectExists(string bucket, string key);
}
```

- [ ] **Step 4: Create R2StorageService implementation**

Create `backend/Infrastructure/Services/R2StorageService.cs`:

```csharp
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

public class R2StorageService : IR2StorageService
{
    private readonly AmazonS3Client _s3Client;
    private readonly string _publicBucket;
    private readonly string _backupBucket;
    private readonly string _publicBucketUrl;

    public string PublicBucket => _publicBucket;
    public string BackupBucket => _backupBucket;
    public string PublicBucketUrl => _publicBucketUrl;

    public R2StorageService(IConfiguration configuration)
    {
        var accountId = configuration["R2:AccountId"] ?? throw new InvalidOperationException("R2:AccountId is required");
        var accessKeyId = configuration["R2:AccessKeyId"] ?? throw new InvalidOperationException("R2:AccessKeyId is required");
        var secretAccessKey = configuration["R2:SecretAccessKey"] ?? throw new InvalidOperationException("R2:SecretAccessKey is required");
        _publicBucket = configuration["R2:PublicBucketName"] ?? "cms-public";
        _backupBucket = configuration["R2:BackupBucketName"] ?? "cms-backup";
        _publicBucketUrl = configuration["R2:PublicBucketUrl"] ?? throw new InvalidOperationException("R2:PublicBucketUrl is required");

        var config = new AmazonS3Config
        {
            ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
            RegionEndpoint = RegionEndpoint.USEast1,
        };

        _s3Client = new AmazonS3Client(accessKeyId, secretAccessKey, config);
    }

    public async Task UploadAsync(string bucket, string key, Stream content, string contentType, CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
        };

        await _s3Client.PutObjectAsync(request, ct);
    }

    public async Task<Stream> DownloadAsync(string bucket, string key, CancellationToken ct = default)
    {
        var request = new GetObjectRequest
        {
            BucketName = bucket,
            Key = key,
        };

        var response = await _s3Client.GetObjectAsync(request, ct);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string bucket, string key, CancellationToken ct = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = bucket,
            Key = key,
        };

        await _s3Client.DeleteObjectAsync(request, ct);
    }

    public bool ObjectExists(string bucket, string key)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = bucket,
                Key = key,
            };

            var response = _s3Client.GetObjectMetadataAsync(request).GetAwaiter().GetResult();
            return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (Amazon.S3.AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
```

- [ ] **Step 5: Build to verify**

```bash
dotnet build
```

Expected: Build succeeds.

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat: add R2StorageService with AWSSDK.S3"
```

---

### Task 2: Update UploadFileCommandHandler to use R2

**Files:**
- Modify: `backend/Application/Files/UploadFileCommandHandler.cs`

- [ ] **Step 1: Rewrite UploadFileCommandHandler**

Replace the local filesystem logic with R2 upload:

```csharp
using Application.Abstractions;
using Domain.Files;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Application.Files;

public class UploadFileCommandHandler(
    CmsDbContext db,
    IR2StorageService r2
) : ICommandHandler<UploadFileCommand, FileResponse>
{
    public async Task<FileResponse> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await request.File.CopyToAsync(memoryStream, cancellationToken);
        var bytes = memoryStream.ToArray();

        var contentHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));

        var existing = await db.FileItems
            .FirstOrDefaultAsync(f => f.ContentHash == contentHash, cancellationToken);

        if (existing is not null)
        {
            return FileResponse.From(existing, isNew: false);
        }

        var fileId = Guid.NewGuid();
        var extension = Path.GetExtension(request.File.FileName).TrimStart('.').ToLowerInvariant();
        var key = $"{fileId}.{extension}";

        using var uploadStream = new MemoryStream(bytes);
        await r2.UploadAsync(r2.PublicBucket, key, uploadStream, request.File.ContentType, cancellationToken);

        var url = $"{r2.PublicBucketUrl}/{key}";

        var fileItem = new FileItem
        {
            Id = fileId,
            FileName = request.File.FileName,
            Extension = extension,
            ContentType = request.File.ContentType,
            Size = request.File.Length,
            StoragePath = key,
            Url = url,
            ContentHash = contentHash,
            AltText = request.AltText,
        };

        db.FileItems.Add(fileItem);
        await db.SaveChangesAsync(cancellationToken);

        return FileResponse.From(fileItem, isNew: true);
    }
}
```

- [ ] **Step 2: Build to verify**

```bash
dotnet build
```

Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add -A && git commit -m "feat: upload files to R2 public bucket"
```

---

### Task 3: Update DeleteFileCommandHandler to use R2

**Files:**
- Modify: `backend/Application/Files/DeleteFileCommandHandler.cs`

- [ ] **Step 1: Rewrite DeleteFileCommandHandler**

Replace local file deletion with R2 deletion:

```csharp
using Application.Abstractions;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Application.Files;

public class DeleteFileCommandHandler(
    CmsDbContext db,
    IR2StorageService r2
) : ICommandHandler<DeleteFileCommand, bool>
{
    public async Task<bool> Handle(DeleteFileCommand request, CancellationToken cancellationToken)
    {
        var file = await db.FileItems
            .Include(f => f.Posts)
            .FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken);

        if (file is null)
        {
            return false;
        }

        if (file.Posts.Count > 0)
        {
            return false;
        }

        await r2.DeleteAsync(r2.PublicBucket, file.StoragePath, cancellationToken);

        db.FileItems.Remove(file);
        await db.SaveChangesAsync(cancellationToken);

        return true;
    }
}
```

- [ ] **Step 2: Build to verify**

```bash
dotnet build
```

Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add -A && git commit -m "feat: delete files from R2 public bucket"
```

---

### Task 4: Create new DatabaseSyncService using R2

**Files:**
- Delete: `backend/Infrastructure/Services/DatabaseSyncService.cs`
- Delete: `backend/Infrastructure/Services/IDatabaseSyncService.cs`
- Create: `backend/Infrastructure/Services/DatabaseSyncService.cs`

- [ ] **Step 1: Rewrite DatabaseSyncService**

Replace `GoogleDriveService` dependency with `IR2StorageService`:

```csharp
using Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class DatabaseSyncService(
    ILogger<DatabaseSyncService> logger,
    IR2StorageService r2,
    IConfiguration configuration
) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(
        configuration.GetValue<int?>("R2:SyncIntervalMinutes") ?? 15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Database sync service started. Sync interval: {Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, stoppingToken);
                await SyncDatabaseAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Database sync service stopping");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during database sync");
            }
        }
    }

    public async Task SyncDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("Sqlite") ?? "Data Source=cms.db";
        var databasePath = connectionString.Replace("Data Source=", "");

        if (!File.Exists(databasePath))
        {
            logger.LogWarning("Database file not found at {Path}", databasePath);
            return;
        }

        var tempDbPath = Path.GetTempFileName();
        try
        {
            logger.LogInformation("Starting database sync to R2");

            using (var sourceDb = new SqliteConnection($"Data Source={databasePath}"))
            using (var destinationDb = new SqliteConnection($"Data Source={tempDbPath};Pooling=False;"))
            {
                sourceDb.Open();
                destinationDb.Open();
                sourceDb.BackupDatabase(destinationDb);
            }

            var dbFileName = Path.GetFileName(databasePath);
            await using var fileStream = new FileStream(tempDbPath, FileMode.Open, FileAccess.Read);
            await r2.UploadAsync(r2.BackupBucket, dbFileName, fileStream, "application/vnd.sqlite3", cancellationToken);

            logger.LogInformation("Database sync completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to sync database to R2");
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
}
```

- [ ] **Step 2: Build to verify**

```bash
dotnet build
```

Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add -A && git commit -m "feat: database sync to R2 backup bucket"
```

---

### Task 5: Create new DatabaseRestoreService using R2

**Files:**
- Delete: `backend/Infrastructure/Services/DatabaseRestoreService.cs`
- Create: `backend/Infrastructure/Services/DatabaseRestoreService.cs`

- [ ] **Step 1: Rewrite DatabaseRestoreService**

Replace `GoogleDriveService` dependency with `IR2StorageService`:

```csharp
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class DatabaseRestoreService(
    ILogger<DatabaseRestoreService> logger,
    IR2StorageService r2,
    IConfiguration configuration
) : IHostedService
{
    private readonly int _maxRetries = configuration.GetValue<int?>("R2:RestoreMaxRetries") ?? 3;

    public static void EnsureDatabaseExists(IR2StorageService r2, IConfiguration configuration, ILogger<DatabaseRestoreService> logger)
    {
        var connectionString = configuration.GetConnectionString("Sqlite") ?? "Data Source=cms.db";
        var databasePath = connectionString.Replace("Data Source=", "");

        if (File.Exists(databasePath))
        {
            logger.LogInformation("Database already exists at {Path}, skipping restore", databasePath);
            return;
        }

        logger.LogInformation("No database found at {Path}, attempting restore from R2", databasePath);

        var fileName = Path.GetFileName(databasePath);
        var maxRetries = configuration.GetValue<int?>("R2:RestoreMaxRetries") ?? 3;
        var cancellationToken = CancellationToken.None;

        try
        {
            var task = RestoreDatabaseAsyncInternal(r2, fileName, maxRetries, databasePath, logger, cancellationToken);
            task.Wait(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database restore failed, will continue with new database");
        }
    }

    private static async Task RestoreDatabaseAsyncInternal(IR2StorageService r2, string fileName, int maxRetries, string databasePath, ILogger<DatabaseRestoreService> logger, CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                logger.LogInformation("Attempting to restore database (attempt {Attempt}/{MaxRetries})", attempt, maxRetries);

                if (!r2.ObjectExists(r2.BackupBucket, fileName))
                {
                    logger.LogWarning("No backup found in R2: {FileName}, creating new database", fileName);
                    return;
                }

                var stream = await r2.DownloadAsync(r2.BackupBucket, fileName, cancellationToken);
                await using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream, cancellationToken);

                var directory = Path.GetDirectoryName(databasePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllBytesAsync(databasePath, memoryStream.ToArray(), cancellationToken);

                logger.LogInformation("Database restored successfully from R2");
                return;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                logger.LogWarning(ex, "Database restore attempt {Attempt} failed, retrying in {Delay}s", attempt, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Database restore failed after {MaxRetries} attempts", maxRetries);
                throw;
            }
        }
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

- [ ] **Step 2: Build to verify**

```bash
dotnet build
```

Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add -A && git commit -m "feat: database restore from R2 backup bucket"
```

---

### Task 6: Clean up Infrastructure DI and delete Google Drive files

**Files:**
- Delete: `backend/Infrastructure/Services/GoogleDriveService.cs`
- Delete: `backend/Infrastructure/Services/GoogleDriveAuthService.cs`
- Delete: `backend/Infrastructure/Services/GoogleTokenProvider.cs`
- Delete: `backend/Infrastructure/Services/IGoogleTokenProvider.cs`
- Modify: `backend/Infrastructure/DependancyInjection.cs`

- [ ] **Step 1: Delete old Google Drive service files**

```bash
rm Infrastructure/Services/GoogleDriveService.cs \
   Infrastructure/Services/GoogleDriveAuthService.cs \
   Infrastructure/Services/GoogleTokenProvider.cs \
   Infrastructure/Services/IGoogleTokenProvider.cs
```

- [ ] **Step 2: Rewrite Infrastructure DI**

Replace the entire Google Drive auth/DI flow with a simple R2 registration:

```csharp
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

        var r2Logger = builder.Services.BuildServiceProvider()
            .GetRequiredService<ILogger<DatabaseRestoreService>>();
        var r2 = builder.Services.BuildServiceProvider()
            .GetRequiredService<IR2StorageService>();
        DatabaseRestoreService.EnsureDatabaseExists(r2, builder.Configuration, r2Logger);

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
```

- [ ] **Step 3: Build to verify**

```bash
dotnet build
```

Expected: Build succeeds (no more Google Drive references).

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: remove Google Drive services, register R2StorageService"
```

---

### Task 7: Update Program.cs — remove static file middleware

**Files:**
- Modify: `backend/Host/Program.cs`

- [ ] **Step 1: Remove static file serving block**

Remove lines 27-56 from Program.cs (the `uploadsPath` directory creation and `UseStaticFiles` middleware). Also remove unused imports:

```csharp
using Api;
using Application;
using Host.Middleware;
using Host.OpenApi;
using Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

builder
    .AddApi()
    .AddApplication()
    .AddInfrastructure();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();
app.UseMiddleware<GlobalExceptionMiddleware>();

app
    .MapApi()
    .MapApplication()
    .MapInfrastructure();

app.Run();
```

- [ ] **Step 2: Delete the uploads directory**

```bash
rm -rf uploads
```

- [ ] **Step 3: Build to verify**

```bash
dotnet build
```

Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: remove static file middleware and uploads directory"
```

---

### Task 8: Update appsettings.json — replace Google Drive with R2 config

**Files:**
- Modify: `backend/Host/appsettings.json`

- [ ] **Step 1: Replace GoogleDrive section with R2**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "Sqlite": "Data Source=cms.db"
  },
  "Auth0": {
    "Domain": "<Enter your Auth0 domain>",
    "Audience": "<Enter your Auth0 audience>"
  },
  "Groq": {
    "ApiKey": "<Enter your Groq API key>"
  },
  "MediatR": {
    "LicenseKey": "<Enter your MediatR license key here>"
  },
  "R2": {
    "AccountId": "<Enter your R2 Account ID>",
    "AccessKeyId": "<Enter your R2 Access Key ID>",
    "SecretAccessKey": "<Enter your R2 Secret Access Key>",
    "PublicBucketName": "cms-public",
    "PublicBucketUrl": "<Enter your R2 public bucket URL>",
    "BackupBucketName": "cms-backup",
    "SyncIntervalMinutes": 15
  }
}
```

- [ ] **Step 2: Subtract: Commit**

```bash
git add -A && git commit -m "config: replace GoogleDrive with R2 config"
```

---

### Task 9: Update frontend to handle absolute R2 URLs

**Files:**
- Modify: `frontend/src/services/upload.ts`

- [ ] **Step 1: Update upload.ts to not prepend API URL when URL is absolute**

The URL returned from the backend will now be a full R2 public URL like `https://pub-xxxx.r2.dev/file.webp`. The existing usages in `Write.vue` and `useImageInsert.ts` use the URL directly for `<img src>` tags. We just need to make sure no code is prepending `VITE_API_URL` when it's already absolute.

Search for `VITE_API_URL` usage in files that handle upload responses:

The current code returns `response.json()` directly and callers use `response.url` as the image source. Since the URL is now absolute (`https://...`), the existing `<img :src="url">` in Vue will work as-is. No frontend changes needed.

- [ ] **Step 2: Verify no changes needed by checking frontend build**

```bash
cd frontend && npm run build
```

Expected: Build succeeds.

- [ ] **Step 3: Commit (if any changes made)**

Only commit if changes were made to the frontend.

---

### Task 10: Update docker-compose.yml, .env.example, and CI/CD

**Files:**
- Modify: `backend/docker-compose.yml`
- Modify: `backend/.env.example`
- Modify: `.github/workflows/ci.yml`

- [ ] **Step 1: Update docker-compose.yml**

Replace all `GoogleDrive__*` env vars with `R2__*`:

```yaml
services:
  api:
    image: stelele/cms-backend:latest
    init: true
    ports:
      - "8002:8000"
    volumes:
      - cms_data:/app/data
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8000
      - ConnectionStrings__Sqlite=Data Source=/app/data/cms.db
      - Auth0__Domain={{AUTH0_DOMAIN}}
      - Auth0__Audience={{AUTH0_AUDIENCE}}
      - GROQ__ApiKey={{GROQ_APIKEY}}
      - MediatR__LicenseKey={{MEDIATR_LICENSEKEY}}
      - R2__AccountId={{R2_ACCOUNT_ID}}
      - R2__AccessKeyId={{R2_ACCESS_KEY_ID}}
      - R2__SecretAccessKey={{R2_SECRET_ACCESS_KEY}}
      - R2__PublicBucketName={{R2_PUBLIC_BUCKET_NAME}}
      - R2__PublicBucketUrl={{R2_PUBLIC_BUCKET_URL}}
      - R2__BackupBucketName={{R2_BACKUP_BUCKET_NAME}}
      - R2__SyncIntervalMinutes={{R2_SYNC_INTERVAL}}
    restart: unless-stopped

volumes:
  cms_data:
    name: cms_data
    external: true
```

- [ ] **Step 2: Update .env.example**

```env
# MediatR License Key
MEDIATR_LICENSE_KEY = {{Enter your MediatR license key here}}

# Groq API Key
GROQ_API_KEY = {{Enter your Groq API key here}}

# Auth0 Configuration
AUTH0_DOMAIN = {{Enter your Auth0 domain here}}
AUTH0_AUDIENCE = {{Enter your Auth0 audience here}}

# R2 Configuration
R2_ACCOUNT_ID = {{Enter your R2 Account ID here}}
R2_ACCESS_KEY_ID = {{Enter your R2 Access Key ID here}}
R2_SECRET_ACCESS_KEY = {{Enter your R2 Secret Access Key here}}
R2_PUBLIC_BUCKET_NAME = cms-public
R2_PUBLIC_BUCKET_URL = {{Enter your R2 public bucket URL here}}
R2_BACKUP_BUCKET_NAME = cms-backup
R2_SYNC_INTERVAL = 15
```

- [ ] **Step 3: Update CI/CD — replace Google Drive secret references with R2**

In `.github/workflows/ci.yml`, replace the `sed` substitution block for Google Drive:

```yaml
      - name: Replace docker-compose placeholders
        run: |
          sed -i \
            -e 's|{{AUTH0_DOMAIN}}|${{ secrets.AUTH0_DOMAIN }}|g' \
            -e 's|{{AUTH0_AUDIENCE}}|${{ secrets.AUTH0_AUDIENCE }}|g' \
            -e 's|{{GROQ_APIKEY}}|${{ secrets.GROQ_API_KEY }}|g' \
            -e 's|{{MEDIATR_LICENSEKEY}}|${{ secrets.MEDIATR_LICENSE_KEY }}|g' \
            -e 's|{{R2_ACCOUNT_ID}}|${{ secrets.R2_ACCOUNT_ID }}|g' \
            -e 's|{{R2_ACCESS_KEY_ID}}|${{ secrets.R2_ACCESS_KEY_ID }}|g' \
            -e 's|{{R2_SECRET_ACCESS_KEY}}|${{ secrets.R2_SECRET_ACCESS_KEY }}|g' \
            -e 's|{{R2_PUBLIC_BUCKET_NAME}}|${{ secrets.R2_PUBLIC_BUCKET_NAME }}|g' \
            -e 's|{{R2_PUBLIC_BUCKET_URL}}|${{ secrets.R2_PUBLIC_BUCKET_URL }}|g' \
            -e 's|{{R2_BACKUP_BUCKET_NAME}}|${{ secrets.R2_BACKUP_BUCKET_NAME }}|g' \
            -e 's|{{R2_SYNC_INTERVAL}}|${{ secrets.R2_SYNC_INTERVAL }}|g' \
            backend/docker-compose.yml
```

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "config: update docker-compose, env.example, and CI for R2"
```

---

### Task 11: Final build and verify

- [ ] **Step 1: Full build**

```bash
cd backend && dotnet build
```

Expected: Build succeeds with no warnings about missing Google Drive types.

- [ ] **Step 2: Verify no remaining Google Drive references**

```bash
cd backend && grep -r "GoogleDrive" --include="*.cs" --include="*.json" --include="*.yml" --include="*.yaml" --include="*.env*" . || echo "No GoogleDrive references found"
```

Expected: `No GoogleDrive references found`.

- [ ] **Step 3: Final commit**

```bash
git add -A && git commit -m "chore: final cleanup after R2 migration"
```
