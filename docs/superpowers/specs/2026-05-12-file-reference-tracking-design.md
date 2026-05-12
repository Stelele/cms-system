# File Reference Tracking & Orphan Cleanup Design

## Problem

When a blog post's content or cover image changes, previously referenced images can become orphaned. The system has no mechanism to:

1. Track which `FileItem` a post's `CoverImageUrl` references
2. Reconcile inline image references in Markdown content against the `PostFiles` join table
3. Clean up `FileItem` records and R2 objects when they are no longer referenced by any post
4. Provide a safety window before deletion to recover from accidental edits

## Design

### Data Model

Add a single nullable field to `FileItem`:

```
MarkedForDeletionAt: DateTimeOffset?  // nullable, set when file has 0 references
```

- When set, the file is scheduled for background deletion after a grace period
- When a file is re-referenced before deletion, the flag is cleared
- Add a non-clustered index on this column for the background service query

### Deduplication

The existing SHA256 hash deduplication on `FileItem.ContentHash` (unique index) remains unchanged. Multiple posts can reference the same `FileItem` via the M:N `PostFiles` join table — this is correct and desired.

### URL Parsing

A static utility in `Application/Files/` that extracts image URLs from post content:

```csharp
public static class ImageUrlExtractor
{
    public static List<string> ExtractImageUrls(string? markdownContent, string? coverImageUrl, string publicBucketUrlPrefix)
}
```

Parses two formats:
- **Markdown**: `![alt](url)` and `![alt](url "title")` via regex
- **HTML**: `<img src="url" ...>` via regex (defensive — TipTap stores Markdown but content could vary)

Only returns URLs starting with `publicBucketUrlPrefix` to filter for our own uploaded files. This means external image URLs are ignored (they aren't stored as FileItems anyway).

### FileReferenceService (Application Layer)

A new `Application/Files/FileReferenceService.cs` injected into post handlers:

```csharp
public class FileReferenceService(CmsDbContext db, IR2StorageService r2)
```

**`ReconcilePostFilesAsync(Guid postId, string? content, string? coverImageUrl, CancellationToken ct)`**

Called after a post is created or updated. Steps:

1. **Extract**: Call `ImageUrlExtractor` on content + cover image to get current reference URLs
2. **Find**: Query `db.FileItems.Where(f => urls.Contains(f.Url))` to get matching FileItems
3. **Load**: Eagerly load the post's current PostFiles associations
4. **Diff**: Compute which FileItems to add (in URLs but not in current associations) vs remove (in current associations but not in URLs)
5. **Reconcile**:
   - Add new associations for files now referenced
   - Remove stale associations for files no longer referenced
   - For each file that lost its last association: set `MarkedForDeletionAt = UtcNow + grace period`
   - For each file that was re-referenced: clear `MarkedForDeletionAt`
6. **Save**: `await db.SaveChangesAsync(ct)`

**`MarkOrphanedFilesAsync(CancellationToken ct)`**

Called after a post is deleted (or any operation where associations are removed). The cascade delete on `PostFiles` already removes join records. This method finds FileItems whose post count dropped to 0 and marks them for deletion. Steps:

1. Query `db.FileItems.Where(f => !f.Posts.Any())` — these are now orphaned
2. Set `MarkedForDeletionAt = UtcNow + grace period` on each
3. Save

### Handler Changes

**`CreatePostCommandHandler`**: After `SaveChangesAsync`, call `FileReferenceService.ReconcilePostFilesAsync(post.Id, post.Content, post.CoverImageUrl, ct)`.

**`UpdatePostCommandHandler`**: After `SaveChangesAsync`, call `FileReferenceService.ReconcilePostFilesAsync(post.Id, post.Content, post.CoverImageUrl, ct)`.

**`DeletePostCommandHandler`**: After `db.Posts.Remove(post)` + `SaveChangesAsync`, call `FileReferenceService.MarkOrphanedFilesAsync(ct)`.

**`DeleteFileCommandHandler`**: Keep immediate deletion for explicit user-initiated deletes. The safety grace period is for automatic dereferencing during post edits. If the file still has references, block deletion (current behavior).

**`AssociateFileWithPostCommandHandler`**: Keep as-is for backward compatibility and programmatic use. The reconciliation on save doesn't conflict — if someone uses the explicit endpoint and then saves the post, the reconciliation will produce the same result.

### FileCleanupService (Background Service)

A new `Infrastructure/Services/FileCleanupService.cs` following the same pattern as `DatabaseSyncService`:

```csharp
public class FileCleanupService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<FileCleanupService> logger
) : BackgroundService
```

- **Interval**: Configurable via `FileCleanup:CleanupIntervalMinutes` (default: 60 minutes)
- **Grace period**: Configurable via `FileCleanup:DeletionGracePeriodHours` (default: 24 hours)

Loop:

1. Delay for interval
2. Create scope, resolve `CmsDbContext` and `IR2StorageService`
3. Query `FileItems.Where(f => f.MarkedForDeletionAt != null && f.MarkedForDeletionAt <= UtcNow)`
4. For each file:
   - Safety check: eagerly load `file.Posts` to confirm count is still 0
   - If re-referenced, clear `MarkedForDeletionAt` and continue
   - Delete from R2: `r2.DeleteAsync(r2.PublicBucket, file.StoragePath, ct)`
   - Remove from DB: `db.FileItems.Remove(file)`
5. `SaveChangesAsync`
6. On error: log and continue (will retry next cycle)

Error handling per the `DatabaseSyncService` pattern — catch exceptions, log, continue loop.

### Configuration

Add to `appsettings.json`:

```json
"FileCleanup": {
  "CleanupIntervalMinutes": 60,
  "DeletionGracePeriodHours": 24
}
```

### DI Registration

In `Infrastructure/DependancyInjection.cs`, register alongside `DatabaseSyncService`:

```csharp
builder.Services.AddSingleton<FileCleanupService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<FileCleanupService>());
```

`FileReferenceService` is scoped (uses `CmsDbContext` directly):

```csharp
builder.Services.AddScoped<FileReferenceService>();
```

### Error Handling & Edge Cases

| Scenario | Handling |
|----------|----------|
| R2 delete fails | Log error, file stays in DB with `MarkedForDeletionAt` set. Retried next cycle. |
| File re-referenced between mark and sweep | Background service's safety check (`Posts.Count == 0`) catches this; clears the flag. |
| Concurrent post updates | EF Core change tracking — reconciliation computes correct diff from current state. |
| Post delete with cascade | Cascade removes PostFiles entries before MarkOrphanedFilesAsync runs. Query finds zero-association files correctly. |
| Blog delete (if added later) | Would cascade to posts → posts cascade to PostFiles. Same MarkOrphanedFilesAsync logic applies. |
| Manual file delete still works | `DeleteFileCommandHandler` unchanged — immediate delete for explicit user action. |

### Out of Scope

- **Blog delete endpoint**: No blog deletion exists in the API currently. If added later, it should cascade-delete posts and call the same orphan logic.
- **Frontend changes**: The `associateFileWithPost` call can remain — reconciliation on save makes it redundant but harmless.
- **Migration for existing orphaned files**: Not addressed. Existing orphaned FileItems would need a one-time script or would be picked up naturally as posts are edited and reconciled.

### Testing

- **Unit tests** for `ImageUrlExtractor` (Markdown parsing, HTML parsing, prefix filtering, empty/null content)
- **Unit tests** for `FileReferenceService.ReconcilePostFilesAsync` (add new refs, remove stale refs, mark/unmark for deletion)
- **Integration tests** for `CreatePostCommandHandler` + `UpdatePostCommandHandler` (verify PostFiles table after save)
- **Integration tests** for `DeletePostCommandHandler` (verify orphan marking)
- **Unit tests** for `FileCleanupService` logic (orchestration, safety check, error handling)
