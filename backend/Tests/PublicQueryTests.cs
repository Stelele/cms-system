using Application.Blogs;
using Application.Posts;

namespace Tests;

public class PublicQueryTests
{
    [Fact]
    public async Task GetPublicPostsByBlog_ExcludesDrafts()
    {
        using var db = new TestDb();
        var blog = db.SeedBlog("Archive", "archive", "html");
        db.SeedPost(blog, "Live", "live", isPublished: true);
        db.SeedPost(blog, "Draft", "draft", isPublished: false);

        var result = await new GetPublicPostsByBlogQueryHandler(db.Db)
            .Handle(new GetPublicPostsByBlogQuery(blog.Id), CancellationToken.None);

        var only = Assert.Single(result);
        Assert.Equal("live", only.Slug);
    }

    [Fact]
    public async Task GetPublicPostBySlug_ReturnsNullForADraft()
    {
        using var db = new TestDb();
        var blog = db.SeedBlog("Archive", "archive", "html");
        db.SeedPost(blog, "Draft", "draft", isPublished: false);

        var result = await new GetPublicPostBySlugQueryHandler(db.Db)
            .Handle(new GetPublicPostBySlugQuery("draft"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPublicPostBySlug_ReturnsAPublishedPost()
    {
        using var db = new TestDb();
        var blog = db.SeedBlog("Archive", "archive", "html");
        db.SeedPost(blog, "Live", "live", isPublished: true);

        var result = await new GetPublicPostBySlugQueryHandler(db.Db)
            .Handle(new GetPublicPostBySlugQuery("live"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Live", result.Title);
    }

    [Fact]
    public async Task GetPublicPostsByBlog_OrdersNewestFirst()
    {
        using var db = new TestDb();
        var blog = db.SeedBlog("Archive", "archive", "html");
        db.SeedPost(blog, "Older", "older", publishedOn: new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));
        db.SeedPost(blog, "Newer", "newer", publishedOn: new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var result = await new GetPublicPostsByBlogQueryHandler(db.Db)
            .Handle(new GetPublicPostsByBlogQuery(blog.Id), CancellationToken.None);

        Assert.Equal(["newer", "older"], result.Select(p => p.Slug).ToArray());
    }

    [Fact]
    public async Task GetPublicBlogs_FiltersBySlug()
    {
        using var db = new TestDb();
        db.SeedBlog("Game Dev Projects", "game-dev");
        db.SeedBlog("Graphics Projects", "graphics");

        var result = await new GetPublicBlogsQueryHandler(db.Db)
            .Handle(new GetPublicBlogsQuery(["graphics"]), CancellationToken.None);

        var only = Assert.Single(result);
        Assert.Equal("graphics", only.Slug);
    }
}
