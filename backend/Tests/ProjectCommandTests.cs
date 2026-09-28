using Application.Blogs;
using Application.Projects;
using Domain.Blogs;
using Domain.Posts;

namespace Tests;

public class ProjectCommandTests
{
    [Fact]
    public async Task Create_StoresEveryTypedField()
    {
        using var db = new TestDb();
        var blog = db.SeedBlog("Graphics", "graphics", "markdown", BlogKind.Project);

        var handler = new CreateProjectCommandHandler(db.Db, db.FileReferenceService());
        var id = await handler.Handle(
            new CreateProjectCommand(
                blog.Id, "Shader Land", "shader-land", "Body", "A shader toy clone",
                ProjectCategory.Graphics, 2025, ["Vue", "WebGPU"], null,
                [new ProjectLinkInput("Source", "https://github.com/Stelele/shader-land")],
                "https://cdn.hashnode.com/x.png", IsPublished: true,
                PublishedOn: new DateTimeOffset(2025, 2, 8, 0, 0, 0, TimeSpan.Zero)),
            CancellationToken.None);

        db.Db.ChangeTracker.Clear();
        var project = db.Db.Posts.OfType<Project>().Single(p => p.Id == id);

        Assert.Equal(ProjectCategory.Graphics, project.Category);
        Assert.Equal(2025, project.Year);
        Assert.Equal(["Vue", "WebGPU"], project.Stack);
        Assert.Equal("graphics", project.Tag);
        Assert.Equal("https://cdn.hashnode.com/x.png", project.CoverImageUrl);
        Assert.Single(project.Links);
        Assert.Equal(new DateTimeOffset(2025, 2, 8, 0, 0, 0, TimeSpan.Zero), project.PublishedOn);
    }

    [Fact]
    public async Task Create_IntoAStandardBlog_IsRejected()
    {
        using var db = new TestDb();
        var blog = db.SeedBlog("Random", "random");

        var handler = new CreateProjectCommandHandler(db.Db, db.FileReferenceService());

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new CreateProjectCommand(blog.Id, "Nope", "nope", "Body", null,
                ProjectCategory.Graphics, 2024, [], null, [], null, true, null),
            CancellationToken.None));
    }

    [Fact]
    public async Task Create_WithACategoryThatDoesNotMatchItsBlog_IsRejected()
    {
        // The plan's original test for this only asserted that two slugs differ,
        // which validates nothing. This is the real invariant: the denormalised
        // Category is only trustworthy because a mismatch is refused.
        using var db = new TestDb();
        var blog = db.SeedBlog("Graphics", "graphics", "markdown", BlogKind.Project);

        var handler = new CreateProjectCommandHandler(db.Db, db.FileReferenceService());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new CreateProjectCommand(blog.Id, "Wrong Category", "wrong", "Body", null,
                ProjectCategory.GameDev, 2024, [], null, [], null, true, null),
            CancellationToken.None));

        Assert.Contains("game-dev", ex.Message);
        Assert.Empty(db.Db.Posts);
    }

    [Fact]
    public async Task Update_ChangesTheTypedFields()
    {
        using var db = new TestDb();
        var blog = db.SeedBlog("Graphics", "graphics", "markdown", BlogKind.Project);
        var seeded = db.SeedProject(blog, "Old", "old", ProjectCategory.Graphics, year: 2024);

        var handler = new UpdateProjectCommandHandler(db.Db, db.FileReferenceService());
        var ok = await handler.Handle(
            new UpdateProjectCommand(blog.Id, seeded.Id, "New", "new", "Body2", "Brief2",
                ProjectCategory.Graphics, 2026, ["Go"], null, [], null, true, null),
            CancellationToken.None);

        Assert.True(ok);
        db.Db.ChangeTracker.Clear();
        var project = db.Db.Posts.OfType<Project>().Single(p => p.Id == seeded.Id);
        Assert.Equal("New", project.Title);
        Assert.Equal(2026, project.Year);
        Assert.Equal(["Go"], project.Stack);
    }

    [Fact]
    public async Task Update_IntoAStandardBlog_IsRejected()
    {
        using var db = new TestDb();
        var standard = db.SeedBlog("Random", "random");
        var seeded = db.SeedPost(standard, "A Post", "a-post");

        var handler = new UpdateProjectCommandHandler(db.Db, db.FileReferenceService());

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new UpdateProjectCommand(standard.Id, seeded.Id, "T", "t", "B", null,
                ProjectCategory.Graphics, 2024, [], null, [], null, true, null),
            CancellationToken.None));
    }

    [Fact]
    public async Task Delete_RemovesOnlyProjects()
    {
        using var db = new TestDb();
        var standard = db.SeedBlog("Random", "random");
        var post = db.SeedPost(standard, "Keep", "keep");
        var projectBlog = db.SeedBlog("Graphics", "graphics", "markdown", BlogKind.Project);
        var project = db.SeedProject(projectBlog, "Remove", "remove", ProjectCategory.Graphics);

        var handler = new DeleteProjectCommandHandler(db.Db);
        Assert.True(await handler.Handle(new DeleteProjectCommand(projectBlog.Id, project.Id), CancellationToken.None));
        Assert.False(await handler.Handle(new DeleteProjectCommand(projectBlog.Id, post.Id), CancellationToken.None));

        db.Db.ChangeTracker.Clear();
        Assert.Single(db.Db.Posts);
    }

    [Fact]
    public async Task CreateBlog_WithAProjectKind_RequiresARegisteredSlug()
    {
        using var db = new TestDb();
        var handler = new CreateBlogCommandHandler(db.Db);

        var id = await handler.Handle(new CreateBlogCommand(
            "Game Dev Projects", "game-dev", "Games", "i-ph-game-controller", BlogKind.Project),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(new CreateBlogCommand(
            "Not A Project Blog", "random", "desc", "i-heroicons-book-open", BlogKind.Project),
            CancellationToken.None));

        // Only the first was created; the second was refused, so it must not exist.
        var blogs = db.Db.Blogs.ToList();
        Assert.Single(blogs);
        Assert.Equal("game-dev", blogs[0].Slug);
        Assert.Equal(BlogKind.Project, blogs[0].Kind);
    }
}
