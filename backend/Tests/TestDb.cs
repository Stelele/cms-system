using Domain.Blogs;
using Domain.Posts;
using Infrastructure.Models;
using Infrastructure.Services;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Tests;

/// <summary>
/// A real CmsDbContext over a throwaway SQLite file. Uses EnsureCreated rather
/// than Migrate so tests exercise the current model without depending on
/// migration history.
/// </summary>
public sealed class TestDb : IDisposable
{
    private readonly string _dir;

    public CmsDbContext Db { get; }

    public TestDb()
    {
        _dir = Path.Combine(Path.GetTempPath(), "cms-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_dir);

        var options = new DbContextOptionsBuilder<CmsDbContext>()
            .UseSqlite($"Data Source={Path.Combine(_dir, "test.db")}")
            .Options;

        try
        {
            Db = new CmsDbContext(options, Mock.Of<IPublisher>());
            Db.Database.EnsureCreated();
        }
        catch
        {
            // A throwing constructor means Dispose() never runs, so the temp
            // directory created just above would be left behind. Delete it here
            // and let the original failure surface.
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, true);
            throw;
        }
    }

    /// <summary>
    /// Seeds a Blog row. A later task adds a contentType parameter, once
    /// Blog.Create accepts one; no such parameter exists at this commit.
    /// </summary>
    public Blog SeedBlog(string name, string slug)
    {
        var blog = Blog.Create(name, slug, $"{name} description", "i-heroicons-book-open");
        Db.Blogs.Add(blog);
        Db.SaveChanges();
        return blog;
    }

    /// <summary>
    /// Seeds a Post row.
    /// </summary>
    /// <param name="publishedOn">
    /// Applied only when <paramref name="isPublished"/> is true; when
    /// <paramref name="isPublished"/> is false it is ignored and the post is
    /// saved unpublished with no date of our choosing. Documented rather than
    /// rejected with an <see cref="ArgumentException"/> because this fixture is
    /// shared by later tasks whose data setup passes both arguments as a
    /// convenience — throwing would turn a benign call into a hard failure.
    /// </param>
    public Post SeedPost(
        Blog blog,
        string title,
        string slug,
        bool isPublished = true,
        string content = "Body text",
        string tag = "general",
        DateTimeOffset? publishedOn = null)
    {
        var post = Post.Create(blog.Id, title, slug, content, $"{title} brief", tag);
        if (isPublished)
        {
            post.Publish();
            if (publishedOn.HasValue)
                post.PublishedOn = publishedOn.Value;
        }
        // publishedOn is deliberately not applied when isPublished is false:
        // see the <param> docs above.

        Db.Posts.Add(post);
        Db.SaveChanges();
        return post;
    }

    /// <summary>
    /// A real FileReferenceService over this context. Its
    /// ReconcilePostFilesAsync is not virtual, so it cannot be mocked; the real
    /// implementation only queries the database and reads r2.PublicBucketUrl,
    /// so a mocked IR2StorageService is sufficient and makes no network calls.
    /// </summary>
    public FileReferenceService FileReferenceService()
    {
        var r2 = new Mock<IR2StorageService>();
        r2.SetupGet(x => x.PublicBucketUrl).Returns("https://cdn.example.test");
        return new FileReferenceService(Db, r2.Object);
    }

    public void Dispose()
    {
        Db.Dispose();
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, true);
    }
}
