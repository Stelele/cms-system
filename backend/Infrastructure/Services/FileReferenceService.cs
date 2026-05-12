using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class FileReferenceService(
    CmsDbContext db,
    IR2StorageService r2
)
{
    public async Task ReconcilePostFilesAsync(Guid postId, string? content, string? coverImageUrl, CancellationToken ct)
    {
        var currentUrls = ImageUrlExtractor.ExtractImageUrls(content, coverImageUrl, r2.PublicBucketUrl);
        var currentFiles = await db.FileItems
            .Where(f => currentUrls.Contains(f.Url))
            .ToListAsync(ct);

        var post = await db.Posts
            .Include(p => p.Files)
            .FirstAsync(p => p.Id == postId, ct);

        var currentFileIds = currentFiles.Select(f => f.Id).ToHashSet();
        var existingFileIds = post.Files.Select(f => f.Id).ToHashSet();

        var toAdd = currentFiles.Where(f => !existingFileIds.Contains(f.Id)).ToList();
        var toRemove = post.Files.Where(f => !currentFileIds.Contains(f.Id)).ToList();

        foreach (var file in toAdd)
        {
            post.Files.Add(file);
            if (file.MarkedForDeletionAt.HasValue)
                file.MarkedForDeletionAt = null;
        }

        foreach (var file in toRemove)
        {
            post.Files.Remove(file);
            await db.Entry(file).Collection(f => f.Posts).LoadAsync(ct);
            if (file.Posts.Count == 0)
                file.MarkedForDeletionAt = DateTimeOffset.UtcNow.AddHours(24);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task MarkOrphanedFilesAsync(CancellationToken ct)
    {
        var orphanedFiles = await db.FileItems
            .Where(f => !f.Posts.Any())
            .ToListAsync(ct);

        foreach (var file in orphanedFiles)
        {
            file.MarkedForDeletionAt = DateTimeOffset.UtcNow.AddHours(24);
        }

        await db.SaveChangesAsync(ct);
    }
}
