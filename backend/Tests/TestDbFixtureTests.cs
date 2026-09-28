using Domain.Blogs;

namespace Tests;

public class TestDbFixtureTests
{
    [Fact]
    public void SeedBlog_PersistsAndLoadsBack()
    {
        using var db = new TestDb();
        var seeded = db.SeedBlog("Game Dev Projects", "game-dev");

        // Force a real round-trip: otherwise Single() returns the tracked
        // instance that was just saved and the assertions are vacuous.
        db.Db.ChangeTracker.Clear();

        var loaded = db.Db.Blogs.Single();

        Assert.Equal(seeded.Id, loaded.Id);
        Assert.Equal("game-dev", loaded.Slug);
    }

    [Fact]
    public void SeedPost_PersistsPublishedFlagAndDate()
    {
        using var db = new TestDb();
        var blog = db.SeedBlog("Blog", "blog");
        // Non-zero offset: DateTimeOffsetToBinaryConverter drops offset
        // information, so only a non-zero offset can detect that loss.
        var when = new DateTimeOffset(2024, 5, 18, 14, 30, 0, TimeSpan.FromHours(5));

        var post = db.SeedPost(blog, "A Post", "a-post", isPublished: true, publishedOn: when);

        db.Db.ChangeTracker.Clear();

        var loaded = db.Db.Posts.Single();
        Assert.True(loaded.IsPublished);
        Assert.Equal(when, loaded.PublishedOn);
    }
}
