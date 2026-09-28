using Domain.Blogs;
using Domain.Posts;
using Infrastructure.Models;
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

        Db = new CmsDbContext(options, Mock.Of<IPublisher>());
        Db.Database.EnsureCreated();
    }

    /// <summary>
    /// Gained its contentType parameter in Task 1, once Blog.Create accepted one.
    /// </summary>
    public Blog SeedBlog(string name, string slug)
    {
        var blog = Blog.Create(name, slug, $"{name} description", "i-heroicons-book-open");
        Db.Blogs.Add(blog);
        Db.SaveChanges();
        return blog;
    }

    public Post SeedPost(
        Blog blog,
        string title,
        string slug,
        bool isPublished = true,
        string? content = "Body text",
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

        Db.Posts.Add(post);
        Db.SaveChanges();
        return post;
    }

    public void Dispose()
    {
        Db.Dispose();
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, true);
    }
}
