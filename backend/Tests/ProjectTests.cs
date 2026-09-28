using Domain.Blogs;
using Domain.Posts;

namespace Tests;

/// <summary>
/// Guards the promise that this feature changes nothing about ordinary posts.
/// </summary>
public class ProjectTests
{
    [Fact]
    public void Blog_WithoutAnExplicitKind_IsStandard()
    {
        var blog = Blog.Create("Test", "test", "desc", "i-heroicons-book-open");

        Assert.Equal(BlogKind.Standard, blog.Kind);
    }

    [Fact]
    public void Post_StillRoundTripsThroughTheDatabase()
    {
        using var db = new TestDb();
        var blog = db.SeedBlog("Plain", "plain");
        var post = db.SeedPost(blog, "Still Works", "still-works");

        db.Db.ChangeTracker.Clear();

        var loaded = db.Db.Posts.Single(p => p.Id == post.Id);
        Assert.Equal("Still Works", loaded.Title);
        Assert.IsNotType<Project>(loaded);
    }

    [Fact]
    public void ProjectBlogs_AreUniquelySlugged()
    {
        var slugs = ProjectBlogs.All.Select(d => d.Slug).ToList();

        Assert.Equal(slugs.Count, slugs.Distinct().Count());
        Assert.Equal(slugs.Count, ProjectBlogs.All.Select(d => d.Category).Distinct().Count());
    }

    [Fact]
    public void ForSlug_RoundTripsEveryCategory()
    {
        foreach (var definition in ProjectBlogs.All)
        {
            Assert.Equal(definition, ProjectBlogs.For(definition.Category));
            Assert.Equal(definition, ProjectBlogs.ForSlug(definition.Slug));
        }
    }

    [Fact]
    public void Project_Create_StampsTheCategorySlugAsTag()
    {
        var project = Project.Create(
            Guid.NewGuid(), "Stick Legends", "stick-legends", "body", "brief",
            ProjectCategory.GameDev, 2025);

        Assert.Equal("game-dev", project.Tag);
        Assert.Equal(ProjectCategory.GameDev, project.Category);
        Assert.Equal(2025, project.Year);
    }

    [Fact]
    public void Project_IsAPost()
    {
        Assert.True(typeof(Post).IsAssignableFrom(typeof(Project)));
    }
}
