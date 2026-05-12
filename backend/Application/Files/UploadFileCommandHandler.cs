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
        var typeFolder = R2StorageService.GetTypeFolder(request.File.ContentType);
        var key = $"cms-app/{r2.Environment}/{typeFolder}/{fileId}.{extension}";

        memoryStream.Position = 0;
        await r2.UploadAsync(r2.PublicBucket, key, memoryStream, request.File.ContentType, cancellationToken);

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
