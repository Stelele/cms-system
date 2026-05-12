# File Reference Tracking & Orphan Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Track FileItem references per post, reconcile on post save, and clean up orphaned files via a background service.

**Architecture:** Add `MarkedForDeletionAt` to FileItem; create `ImageUrlExtractor` (static utility) and `FileReferenceService` (Application layer) for reconciling PostFiles associations on post create/update/delete; create `FileCleanupService` (BackgroundService in Infrastructure) to periodically delete orphaned files from R2 + DB. Follow existing `DatabaseSyncService` pattern for the background service.

**Tech Stack:** .NET 10, EF Core (SQLite), xUnit + Moq, Cloudflare R2 (S3-compatible)

---

### Task 1: Add MarkedForDeletionAt to FileItem entity

**Files:**
- Modify: `Domain/Files/FileItem.cs`
- Modify: `Infrastructure/Models/FileEntity.cs`

- [ ] **Step 1: Add property to FileItem**

Add to `Domain/Files/FileItem.cs`:

```csharp
public DateTimeOffset? MarkedForDeletionAt { get; set; }
```

- [ ] **Step 2: Add index to FileEntity configuration**

Add to `Infrastructure/Models/FileEntity.cs` after the ContentHash index:

```csharp
builder.HasIndex(f => f.MarkedForDeletionAt);
```

- [ ] **Step 3: Build to verify compilation**

Run: `dotnet build` in `backend/`
Expected: Build succeeds

---

### Task 2: ImageUrlExtractor utility

**Files:**
- Create: `Application/Files/ImageUrlExtractor.cs`

- [ ] **Step 1: Create ImageUrlExtractor**

Create `Application/Files/ImageUrlExtractor.cs`:

```csharp
using System.Text.RegularExpressions;

namespace Application.Files;

public static partial class ImageUrlExtractor
{
    [GeneratedRegex(@"!\[.*?\]\(([^\s""']+)(?:\s+""[^""]*"")?\)")]
    private static partial Regex MarkdownImageRegex();

    [GeneratedRegex(@"<img[^>]*\bsrc\s*=\s*[""']([^""']+)[""'][^>]*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlImageRegex();

    public static List<string> ExtractImageUrls(string? content, string? coverImageUrl, string publicBucketUrlPrefix)
    {
        var urls = new List<string>();

        if (!string.IsNullOrEmpty(coverImageUrl) && coverImageUrl.StartsWith(publicBucketUrlPrefix))
            urls.Add(coverImageUrl);

        if (!string.IsNullOrEmpty(content))
        {
            foreach (Match match in MarkdownImageRegex().Matches(content))
                if (match.Groups[1].Value.StartsWith(publicBucketUrlPrefix))
                    urls.Add(match.Groups[1].Value);

            foreach (Match match in HtmlImageRegex().Matches(content))
                if (match.Groups[1].Value.StartsWith(publicBucketUrlPrefix))
                    urls.Add(match.Groups[1].Value);
        }

        return urls;
    }
}
```

- [ ] **Step 2: Build to verify compilation**

Run: `dotnet build` in `backend/`
Expected: Build succeeds

---

### Task 3: FileReferenceService

**Files:**
- Create: `Application/Files/FileReferenceService.cs`

- [ ] **Step 1: Create the FileReferenceService**

Create `Application/Files/FileReferenceService.cs`:

```csharp
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Application.Files;

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
```

- [ ] **Step 2: Build to verify compilation**

Run: `dotnet build` in `backend/`
Expected: Build succeeds

---

### Task 4: Update post handlers to call FileReferenceService

**Files:**
- Modify: `Application/Posts/CreatePostCommandHandler.cs`
- Modify: `Application/Posts/UpdatePostCommandHandler.cs`
- Modify: `Application/Posts/DeletePostCommandHandler.cs`

- [ ] **Step 1: Update CreatePostCommandHandler**

Add `FileReferenceService` as a primary constructor parameter and call after save:

```csharp
public class CreatePostCommandHandler(
    CmsDbContext db,
    FileReferenceService fileRefService
) : ICommandHandler<CreatePostCommand, Guid>
{
    public async Task<Guid> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        // ... existing validation code ...

        await db.Posts.AddAsync(post, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await fileRefService.ReconcilePostFilesAsync(post.Id, post.Content, post.CoverImageUrl, cancellationToken);

        return post.Id;
    }
}
```

- [ ] **Step 2: Update UpdatePostCommandHandler**

Add `FileReferenceService` as a primary constructor parameter and call after save:

```csharp
public class UpdatePostCommandHandler(
    CmsDbContext db,
    FileReferenceService fileRefService
) : ICommandHandler<UpdatePostCommand, bool>
{
    public async Task<bool> Handle(UpdatePostCommand request, CancellationToken cancellationToken)
    {
        // ... existing validation and update logic ...

        await db.SaveChangesAsync(cancellationToken);

        await fileRefService.ReconcilePostFilesAsync(post.Id, post.Content, post.CoverImageUrl, cancellationToken);

        return true;
    }
}
```

- [ ] **Step 3: Update DeletePostCommandHandler**

Add `FileReferenceService` as a primary constructor parameter and call after delete:

```csharp
public class DeletePostCommandHandler(
    CmsDbContext db,
    FileReferenceService fileRefService
) : ICommandHandler<DeletePostCommand, bool>
{
    public async Task<bool> Handle(DeletePostCommand request, CancellationToken cancellationToken)
    {
        var post = await db.Posts
            .FirstOrDefaultAsync(p => p.BlogId == request.BlogId && p.Id == request.Id, cancellationToken);

        if (post == null) return false;

        db.Posts.Remove(post);
        await db.SaveChangesAsync(cancellationToken);

        await fileRefService.MarkOrphanedFilesAsync(cancellationToken);

        return true;
    }
}
```

- [ ] **Step 4: Build to verify compilation**

Run: `dotnet build` in `backend/`
Expected: Build succeeds

---

### Task 5: FileCleanupService (Background Service)

**Files:**
- Create: `Infrastructure/Services/FileCleanupService.cs`

- [ ] **Step 1: Create the background service**

Create `Infrastructure/Services/FileCleanupService.cs`:

```csharp
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
```

- [ ] **Step 2: Build to verify compilation**

Run: `dotnet build` in `backend/`
Expected: Build succeeds

---

### Task 6: DI Registration + Configuration

**Files:**
- Modify: `Infrastructure/DependancyInjection.cs`
- Modify: `Host/appsettings.json`
- Modify: `Host/appsettings.Development.json`

- [ ] **Step 1: Register services in DependancyInjection.cs**

Add after `DatabaseSyncService` registration in `Infrastructure/DependancyInjection.cs`:

```csharp
builder.Services.AddScoped<FileReferenceService>();

builder.Services.AddSingleton<FileCleanupService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<FileCleanupService>());
```

Also add the `using` for `Application.Files` if needed. Check existing imports — the namespace `Application.Files` may need to be added if not imported yet. Look at the existing imports in `DependancyInjection.cs`:

Current imports:
```csharp
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
```

Add:
```csharp
using Application.Files;
```

- [ ] **Step 2: Add FileCleanup config to Host/appsettings.json**

Add after the `R2` block:

```json
"FileCleanup": {
  "CleanupIntervalMinutes": 60,
  "DeletionGracePeriodHours": 24
}
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build` in `backend/`
Expected: Build succeeds

---

### Task 7: Tests

**Files:**
- Modify: `Tests/FileHandlerTests.cs`

- [ ] **Step 1: Add test for ImageUrlExtractor.MarkdownImageUrls**

Add to `Tests/FileHandlerTests.cs`:

```csharp
public class ImageUrlExtractorTests
{
    private const string PublicBucketUrl = "https://pub-xxx.r2.dev";

    [Fact]
    public void ExtractImageUrls_MarkdownImages_ReturnsUrls()
    {
        var content = "Text ![alt](https://pub-xxx.r2.dev/cms-app/local/images/a.jpg) more ![alt2](https://pub-xxx.r2.dev/cms-app/local/images/b.png)";
        var result = ImageUrlExtractor.ExtractImageUrls(content, null, PublicBucketUrl);
        Assert.Equal(2, result.Count);
        Assert.Contains("https://pub-xxx.r2.dev/cms-app/local/images/a.jpg", result);
        Assert.Contains("https://pub-xxx.r2.dev/cms-app/local/images/b.png", result);
    }

    [Fact]
    public void ExtractImageUrls_ExternalUrls_Ignored()
    {
        var content = "Text ![alt](https://external.com/img.jpg)";
        var result = ImageUrlExtractor.ExtractImageUrls(content, null, PublicBucketUrl);
        Assert.Empty(result);
    }

    [Fact]
    public void ExtractImageUrls_HtmlImages_ReturnsUrls()
    {
        var content = "<img src=\"https://pub-xxx.r2.dev/cms-app/local/images/a.jpg\" />";
        var result = ImageUrlExtractor.ExtractImageUrls(content, null, PublicBucketUrl);
        Assert.Single(result);
    }

    [Fact]
    public void ExtractImageUrls_CoverImageUrl_ReturnsUrl()
    {
        var result = ImageUrlExtractor.ExtractImageUrls(null, "https://pub-xxx.r2.dev/cms-app/local/images/cover.jpg", PublicBucketUrl);
        Assert.Single(result);
    }

    [Fact]
    public void ExtractImageUrls_NullContent_ReturnsEmpty()
    {
        var result = ImageUrlExtractor.ExtractImageUrls(null, null, PublicBucketUrl);
        Assert.Empty(result);
    }
}
```

Note: The source-generated regex requires the assembly to enable `GeneratedRegexAttribute` which is available in .NET 7+. In .NET 10 it should work. If tests fail due to source generation, fall back to non-generated `new Regex(...)` with `RegexOptions.Compiled`.

- [ ] **Step 2: Add tests for FileReferenceService.ReconcilePostFilesAsync**

Add to existing `FileHandlerTests` class (inside the existing class, not as a new class):

```csharp
[Fact]
public async Task ReconcilePostFiles_AddsNewAssociation()
{
    var blog = new Domain.Blogs.Blog { Id = Guid.NewGuid(), Name = "B", Slug = "b", Description = "D" };
    var post = new Domain.Posts.Post { Id = Guid.NewGuid(), BlogId = blog.Id, Title = "T", Content = "C", Slug = "s" };
    var file = new FileItem
    {
        Id = Guid.NewGuid(),
        Url = "https://pub-xxx.r2.dev/cms-app/local/images/test.jpg",
        FileName = "test.jpg",
        Extension = "jpg",
        ContentType = "image/jpeg",
        Size = 100,
        StoragePath = "path",
        ContentHash = "HASH",
    };
    _db.Blogs.Add(blog);
    _db.Posts.Add(post);
    _db.FileItems.Add(file);
    await _db.SaveChangesAsync();

    var service = new FileReferenceService(_db, _r2Mock.Object);
    await service.ReconcilePostFilesAsync(post.Id, "![img](https://pub-xxx.r2.dev/cms-app/local/images/test.jpg)", null, default);

    var postReloaded = await _db.Posts.Include(p => p.Files).FirstAsync(p => p.Id == post.Id);
    Assert.Single(postReloaded.Files);
    Assert.Equal(file.Id, postReloaded.Files.First().Id);
}

[Fact]
public async Task ReconcilePostFiles_RemovesStaleAssociation_AndMarksForDeletion()
{
    var blog = new Domain.Blogs.Blog { Id = Guid.NewGuid(), Name = "B", Slug = "b", Description = "D" };
    var post = new Domain.Posts.Post { Id = Guid.NewGuid(), BlogId = blog.Id, Title = "T", Content = "C", Slug = "s" };
    var file = new FileItem
    {
        Id = Guid.NewGuid(),
        Url = "https://pub-xxx.r2.dev/cms-app/local/images/test.jpg",
        FileName = "test.jpg",
        Extension = "jpg",
        ContentType = "image/jpeg",
        Size = 100,
        StoragePath = "path",
        ContentHash = "HASH",
    };
    file.Posts.Add(post);
    _db.Blogs.Add(blog);
    _db.Posts.Add(post);
    _db.FileItems.Add(file);
    await _db.SaveChangesAsync();

    var service = new FileReferenceService(_db, _r2Mock.Object);
    await service.ReconcilePostFilesAsync(post.Id, "content with no images", null, default);

    var fileReloaded = await _db.FileItems.Include(f => f.Posts).FirstAsync(f => f.Id == file.Id);
    Assert.Empty(fileReloaded.Posts);
    Assert.NotNull(fileReloaded.MarkedForDeletionAt);
}

[Fact]
public async Task ReconcilePostFiles_ReReferencedFile_ClearsDeletionMark()
{
    var blog = new Domain.Blogs.Blog { Id = Guid.NewGuid(), Name = "B", Slug = "b", Description = "D" };
    var post = new Domain.Posts.Post { Id = Guid.NewGuid(), BlogId = blog.Id, Title = "T", Content = "C", Slug = "s" };
    var file = new FileItem
    {
        Id = Guid.NewGuid(),
        Url = "https://pub-xxx.r2.dev/cms-app/local/images/test.jpg",
        FileName = "test.jpg",
        Extension = "jpg",
        ContentType = "image/jpeg",
        Size = 100,
        StoragePath = "path",
        ContentHash = "HASH",
        MarkedForDeletionAt = DateTimeOffset.UtcNow.AddHours(1),
    };
    _db.Blogs.Add(blog);
    _db.Posts.Add(post);
    _db.FileItems.Add(file);
    await _db.SaveChangesAsync();

    var service = new FileReferenceService(_db, _r2Mock.Object);
    await service.ReconcilePostFilesAsync(post.Id, "![img](https://pub-xxx.r2.dev/cms-app/local/images/test.jpg)", null, default);

    var fileReloaded = await _db.FileItems.FirstAsync(f => f.Id == file.Id);
    Assert.Null(fileReloaded.MarkedForDeletionAt);
}
```

- [ ] **Step 3: Run tests**

Run: `dotnet test` in `backend/`
Expected: All tests pass

---

### Task 8: Full build and verification

- [ ] **Step 1: Build entire solution**

Run: `dotnet build` in `backend/`
Expected: Build succeeds with no warnings

- [ ] **Step 2: Run all tests**

Run: `dotnet test` in `backend/`
Expected: All existing + new tests pass

- [ ] **Step 3: Add migration (if needed)**

If database migrations are managed manually, add a new migration:

```bash
dotnet ef migrations add AddMarkedForDeletionAt --project Infrastructure/Infrastructure.csproj --startup-project Host/Host.csproj
```

If using auto-migration (the current `MapInfrastructure` calls `db.Database.Migrate()`), run the above to generate the migration file.

Run: `dotnet build` after migration to verify.
