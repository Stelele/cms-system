using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests;

public class DatabaseBackupTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string _tempDir;

    public DatabaseBackupTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _dbPath = Path.Combine(_tempDir, "cms.db");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void GetTypeFolder_ClassifiesCorrectly()
    {
        Assert.Equal("images", R2StorageService.GetTypeFolder("image/jpeg"));
        Assert.Equal("images", R2StorageService.GetTypeFolder("image/png"));
        Assert.Equal("images", R2StorageService.GetTypeFolder("image/webp"));
        Assert.Equal("gifs", R2StorageService.GetTypeFolder("image/gif"));
        Assert.Equal("videos", R2StorageService.GetTypeFolder("video/mp4"));
        Assert.Equal("videos", R2StorageService.GetTypeFolder("video/webm"));
        Assert.Equal("other", R2StorageService.GetTypeFolder("application/pdf"));
        Assert.Equal("other", R2StorageService.GetTypeFolder(""));
        Assert.Equal("other", R2StorageService.GetTypeFolder(null!));
    }

    [Fact]
    public async Task DatabaseSync_CreatesValidBackup()
    {
        CreateSeedDatabase();

        var r2Mock = new Mock<IR2StorageService>();
        r2Mock.Setup(r => r.BackupBucket).Returns("cms-backup");
        r2Mock.Setup(r => r.Environment).Returns("local");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Sqlite"] = $"Data Source={_dbPath}",
                ["R2:SyncIntervalMinutes"] = "999",
            })
            .Build();

        var logger = new Mock<ILogger<DatabaseSyncService>>();
        var syncService = new DatabaseSyncService(logger.Object, r2Mock.Object, config);

        var backupBytes = new byte[0];
        r2Mock
            .Setup(r => r.UploadAsync(r2Mock.Object.BackupBucket,
                It.Is<string>(k => k == "cms-app/local/database/cms.db"),
                It.IsAny<Stream>(), "application/vnd.sqlite3", default))
            .Returns<string, string, Stream, string, CancellationToken>((_, _, stream, _, _) =>
            {
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                backupBytes = ms.ToArray();
                return Task.CompletedTask;
            });

        await syncService.SyncDatabaseAsync(default);

        Assert.NotEmpty(backupBytes);

        var tempBackupPath = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(tempBackupPath, backupBytes);

            using var conn = new SqliteConnection($"Data Source={tempBackupPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table'";
            var tableCount = (long)cmd.ExecuteScalar()!;
            Assert.True(tableCount > 0, "Backup should contain tables");

            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name";
            using var reader = cmd.ExecuteReader();
            var tables = new List<string>();
            while (reader.Read())
                tables.Add(reader.GetString(0));

            Assert.Contains("Blogs", tables);
            Assert.Contains("Posts", tables);
            Assert.Contains("FileItems", tables);
        }
        finally
        {
            if (File.Exists(tempBackupPath))
                File.Delete(tempBackupPath);
        }

        r2Mock.Verify(r => r.UploadAsync("cms-backup",
            "cms-app/local/database/cms.db",
            It.IsAny<Stream>(), "application/vnd.sqlite3", default), Times.Once);
    }

    [Fact]
    public async Task DatabaseRestore_RestoresFromBackup()
    {
        CreateSeedDatabase();

        var r2Mock = new Mock<IR2StorageService>();
        r2Mock.Setup(r => r.BackupBucket).Returns("cms-backup");
        r2Mock.Setup(r => r.Environment).Returns("local");

        var backupPath = Path.Combine(_tempDir, "backup.db");
        File.Copy(_dbPath, backupPath, true);

        r2Mock
            .Setup(r => r.ObjectExistsAsync("cms-backup", "cms-app/local/database/cms.db", default))
            .ReturnsAsync(true);

        r2Mock
            .Setup(r => r.DownloadAsync("cms-backup", "cms-app/local/database/cms.db", default))
            .ReturnsAsync(new FileStream(backupPath, FileMode.Open, FileAccess.Read));

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Sqlite"] = $"Data Source={_dbPath}",
                ["R2:RestoreMaxRetries"] = "1",
            })
            .Build();

        var logger = new Mock<ILogger<DatabaseRestoreService>>();

        DatabaseRestoreService.EnsureDatabaseExists(r2Mock.Object, config, logger.Object);

        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Blogs";
        var blogCount = (long)cmd.ExecuteScalar()!;
        Assert.Equal(2, blogCount);
    }

    [Fact]
    public async Task DatabaseRestore_NoBackup_CreatesNewDb()
    {
        var freshDbPath = Path.Combine(_tempDir, "fresh.db");

        var r2Mock = new Mock<IR2StorageService>();
        r2Mock.Setup(r => r.BackupBucket).Returns("cms-backup");
        r2Mock.Setup(r => r.Environment).Returns("local");

        r2Mock
            .Setup(r => r.ObjectExistsAsync("cms-backup", "cms-app/local/database/fresh.db", default))
            .ReturnsAsync(false);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Sqlite"] = $"Data Source={freshDbPath}",
                ["R2:RestoreMaxRetries"] = "1",
            })
            .Build();

        var logger = new Mock<ILogger<DatabaseRestoreService>>();

        DatabaseRestoreService.EnsureDatabaseExists(r2Mock.Object, config, logger.Object);

        Assert.False(File.Exists(freshDbPath));
    }

    private void CreateSeedDatabase()
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();

        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS "Blogs" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "Name" TEXT NOT NULL,
                "Slug" TEXT NOT NULL,
                "Description" TEXT NOT NULL,
                "CreatedOn" TEXT NOT NULL,
                "UpdatedOn" TEXT
            );
            CREATE TABLE IF NOT EXISTS "Posts" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "BlogId" TEXT NOT NULL,
                "Title" TEXT NOT NULL,
                "Content" TEXT NOT NULL,
                "Slug" TEXT NOT NULL,
                "Tag" TEXT,
                "PublishedOn" TEXT,
                "IsPublished" INTEGER NOT NULL DEFAULT 0,
                "CreatedOn" TEXT NOT NULL,
                "UpdatedOn" TEXT
            );
            CREATE TABLE IF NOT EXISTS "FileItems" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "FileName" TEXT NOT NULL,
                "Extension" TEXT NOT NULL,
                "ContentType" TEXT NOT NULL,
                "Size" INTEGER NOT NULL,
                "StoragePath" TEXT NOT NULL,
                "Url" TEXT NOT NULL,
                "ContentHash" TEXT NOT NULL,
                "AltText" TEXT,
                "CreatedOn" TEXT NOT NULL,
                "UpdatedOn" TEXT
            );
            INSERT OR IGNORE INTO "Blogs" ("Id", "Name", "Slug", "Description", "CreatedOn")
            VALUES ('b1', 'Tech Blog', 'tech', 'A tech blog', datetime('now'));
            INSERT OR IGNORE INTO "Blogs" ("Id", "Name", "Slug", "Description", "CreatedOn")
            VALUES ('b2', 'Personal Blog', 'personal', 'A personal blog', datetime('now'));
            """;

        cmd.ExecuteNonQuery();
    }
}
