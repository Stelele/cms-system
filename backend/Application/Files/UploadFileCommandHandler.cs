using Application.Abstractions;
using Domain.Files;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Files;

public class UploadFileCommandHandler(
    CmsDbContext db,
    string? uploadsBasePath = null
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
        var storagePath = $"uploads/{fileId}.{extension}";
        var url = $"/uploads/{fileId}.{extension}";

        var basePath = uploadsBasePath ?? AppContext.BaseDirectory;
        var filePath = Path.Combine(basePath, storagePath);
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(filePath, bytes, cancellationToken);

        var fileItem = new FileItem
        {
            Id = fileId,
            FileName = request.File.FileName,
            Extension = extension,
            ContentType = request.File.ContentType,
            Size = request.File.Length,
            StoragePath = storagePath,
            Url = url,
            ContentHash = contentHash,
            AltText = request.AltText,
        };

        db.FileItems.Add(fileItem);
        await db.SaveChangesAsync(cancellationToken);

        return FileResponse.From(fileItem, isNew: true);
    }
}
