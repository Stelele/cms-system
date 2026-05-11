# R2 Storage Migration Design

## Summary

Replace Google Drive (database backup) and local filesystem (media uploads) with Cloudflare R2 using two buckets:
- **Public bucket**: CMS media files (photos, uploads)
- **Private bucket**: SQLite database backups

## Configuration

New `appsettings.json` section replacing `GoogleDrive`:

```json
"R2": {
  "AccountId": "",
  "AccessKeyId": "",
  "SecretAccessKey": "",
  "PublicBucketName": "cms-public",
  "PublicBucketUrl": "https://pub-xxxxx.r2.dev",
  "BackupBucketName": "cms-backup",
  "SyncIntervalMinutes": 15
}
```

## NuGet Changes

| Remove | Add |
|--------|-----|
| `Google.Apis.Drive.v3` | `AWSSDK.S3` |
| `Google.Apis.Auth` | |

## Service Layer Changes

| Old | New |
|-----|-----|
| `GoogleDriveService` | `R2StorageService` — wraps `AmazonS3Client`, handles upload/download/delete for both buckets |
| `GoogleDriveAuthService` | Removed |
| `GoogleTokenProvider` + `IGoogleTokenProvider` | Removed |
| `DatabaseSyncService` | Updated to use `R2StorageService` |
| `DatabaseRestoreService` | Updated to use `R2StorageService` |
| `IDatabaseSyncService` | Removed |

### R2StorageService

```
R2StorageService
├── UploadFileAsync(bucket, key, stream, contentType) → void
├── DownloadFileAsync(bucket, key) → Stream
├── DeleteFileAsync(bucket, key) → void
└── GetPublicUrl(key) → string  (constructs full R2 URL)
```

Instantiated with `AmazonS3Client` configured for R2 endpoint:
```
https://{AccountId}.r2.cloudflarestorage.com
```

## File Upload Flow

1. `UploadFileCommandHandler` calls `R2StorageService.UploadFileAsync(publicBucket, key, stream, contentType)`
2. Stores `FileItem.Url` = `{PublicBucketUrl}/{key}` (full absolute URL)
3. `DeleteFileCommandHandler` calls `R2StorageService.DeleteFileAsync(publicBucket, key)`
4. `GetFileByIdQueryHandler` returns the stored URL as-is

## Files to Delete

- `Infrastructure/Services/GoogleDriveService.cs`
- `Infrastructure/Services/GoogleDriveAuthService.cs`
- `Infrastructure/Services/GoogleTokenProvider.cs`
- `Infrastructure/Services/IGoogleTokenProvider.cs`
- `Infrastructure/Services/DatabaseSyncService.cs`
- `Infrastructure/Services/DatabaseRestoreService.cs`
- `Infrastructure/Services/IDatabaseSyncService.cs`

## Files to Modify

- `Infrastructure/DependancyInjection.cs` — remove Google Drive DI, register `R2StorageService`, add `AWSSDK.S3`
- `Host/appsettings.json` — replace `GoogleDrive` section with `R2`
- `Host/appsettings.Development.json` — remove Google Drive refresh token key
- `Host/Program.cs` — remove `UseStaticFiles` + `PhysicalFileProvider` + directory creation
- `Application/Files/UploadFileCommandHandler.cs` — use `R2StorageService` instead of local disk
- `Application/Files/DeleteFileCommandHandler.cs` — use `R2StorageService` instead of `File.Delete`

## Frontend Changes

- `frontend/src/services/upload.ts` — don't prepend `VITE_API_URL` when response URL starts with `http`

## Configuration Propagation

### Docker Compose

Replace `GoogleDrive__*` with:
```yaml
R2__AccountId=
R2__AccessKeyId=
R2__SecretAccessKey=
R2__PublicBucketName=
R2__PublicBucketUrl=
R2__BackupBucketName=
R2__SyncIntervalMinutes=
```

### CI/CD (.github/workflows/ci.yml)

Replace `GOOGLEDRIVE_*` GitHub secrets with:
- `R2_ACCOUNT_ID`
- `R2_ACCESS_KEY_ID`
- `R2_SECRET_ACCESS_KEY`
- `R2_PUBLIC_BUCKET_NAME`
- `R2_PUBLIC_BUCKET_URL`
- `R2_BACKUP_BUCKET_NAME`
- `R2_SYNC_INTERVAL`

Update substitution patterns from `{{GOOGLEDRIVE_*}}` to `{{R2_*}}`.

### .env.example

Replace Google Drive entries with R2 entries.

### docker-compose.yml

Remove `GoogleDrive__` environment variables, add `R2__` environment variables.

## Cleanup

- Delete `uploads/` directory
- Remove Google Drive NuGet packages from `Infrastructure/Infrastructure.csproj`
- Add `AWSSDK.S3` NuGet package to `Infrastructure/Infrastructure.csproj`
