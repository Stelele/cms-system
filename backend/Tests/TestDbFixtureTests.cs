using Domain.Blogs;

namespace Tests;

public class TestDbFixtureTests
{
    [Fact]
    public void SeedBlog_PersistsAndLoadsBack()
    {
        using var db = new TestDb();
        var seeded = db.SeedBlog("Game Dev Projects", "game-dev");

        var loaded = db.Db.Blogs.Single();

        Assert.Equal(seeded.Id, loaded.Id);
        Assert.Equal("game-dev", loaded.Slug);
    }

    [Fact]
    public void SeedPost_PersistsPublishedFlagAndDate()
    {
        using var db = new TestDb();
        var blog = db.SeedBlog("Blog", "blog");
        var when = new DateTimeOffset(2024, 5, 18, 0, 0, 0, TimeSpan.Zero);

        var post = db.SeedPost(blog, "A Post", "a-post", isPublished: true, publishedOn: when);

        var loaded = db.Db.Posts.Single();
        Assert.True(loaded.IsPublished);
        Assert.Equal(when, loaded.PublishedOn);
    }
}
