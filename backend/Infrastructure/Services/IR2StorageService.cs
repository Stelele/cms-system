namespace Infrastructure.Services;

public interface IR2StorageService
{
    string PublicBucket { get; }
    string BackupBucket { get; }
    string PublicBucketUrl { get; }
    string Environment { get; }
    Task UploadAsync(string bucket, string key, Stream content, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string bucket, string key, CancellationToken ct = default);
    Task DeleteAsync(string bucket, string key, CancellationToken ct = default);
    Task<bool> ObjectExistsAsync(string bucket, string key, CancellationToken ct = default);
}
