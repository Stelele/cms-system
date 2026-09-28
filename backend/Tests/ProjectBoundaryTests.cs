using Application.DTOs;
using Application.Posts;
using Application.Projects;
using Domain.Blogs;
using Domain.Posts;

namespace Tests;

public class ProjectBoundaryTests
{
    [Fact]
    // The published-only guarantee belongs to the public read, which is a
    // separate query so the admin read can show drafts. Both sides are covered
    // in ProjectDraftVisibilityTests; asserting it here on the admin query would
    // re-assert the bug that made the admin UI show no projects at all.
    public async Task GetPublicProjects_ExcludesDrafts()
    {
        using var db = new TestDb();
        var blog = db.SeedBlog("Game Dev Projects", "game-dev", "markdown", BlogKind.Project);
        db.SeedProject(blog, "Live", "live", ProjectCategory.GameDev);
        db.SeedProject(blog, "Draft", "draft", ProjectCategory.GameDev, isPublished: false);

        var result = await new GetPublicProjectsQueryHandler(db.Db)
            .Handle(new GetPublicProjectsQuery(null), CancellationToken.None);

        Assert.Equal(["live"], result.Select(p => p.Slug).ToArray());
    }

    [Fact]
    public async Task GetProjects_ExcludesOrdinaryPosts()
    {
        using var db = new TestDb();
        var standard = db.SeedBlog("Random", "random");
        db.SeedPost(standard, "A Post", "a-post");
        var projectBlog = db.SeedBlog("Graphics", "graphics", "markdown", BlogKind.Project);
        db.SeedProject(projectBlog, "Shader Land", "shader-land", ProjectCategory.Graphics);

        var result = await new GetProjectsQueryHandler(db.Db)
            .Handle(new GetProjectsQuery(null), CancellationToken.None);

        Assert.Equal(["shader-land"], result.Select(p => p.Slug).ToArray());
    }

    [Fact]
    public async Task GetProjects_FiltersByCategory()
    {
        using var db = new TestDb();
        var gamedev = db.SeedBlog("Game Dev Projects", "game-dev", "markdown", BlogKind.Project);
        var graphics = db.SeedBlog("Graphics", "graphics", "markdown", BlogKind.Project);
        db.SeedProject(gamedev, "A", "a", ProjectCategory.GameDev);
        db.SeedProject(graphics, "B", "b", ProjectCategory.Graphics);

        var result = await new GetProjectsQueryHandler(db.Db)
            .Handle(new GetProjectsQuery(ProjectCategory.Graphics), CancellationToken.None);

        Assert.Equal(["b"], result.Select(p => p.Slug).ToArray());
    }

    [Fact]
    public async Task GetPublicProjectBySlug_ReturnsNullForADraft()
    {
        using var db = new TestDb();
        var blog = db.SeedBlog("Game Dev Projects", "game-dev", "markdown", BlogKind.Project);
        db.SeedProject(blog, "Draft", "draft", ProjectCategory.GameDev, isPublished: false);

        var result = await new GetPublicProjectBySlugQueryHandler(db.Db)
            .Handle(new GetPublicProjectBySlugQuery("draft"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreatePost_IntoAProjectBlog_IsRejected()
    {
        using var db = new TestDb();
        var blog = db.SeedBlog("Game Dev Projects", "game-dev", "markdown", BlogKind.Project);

        var handler = new CreatePostCommandHandler(db.Db, db.FileReferenceService());

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new CreatePostCommand(blog.Id, "Sneaky", "sneaky", "Body", null, "general", null, true),
            CancellationToken.None));

        Assert.Empty(db.Db.Posts);
    }

    [Fact]
    public async Task CreatePost_IntoAStandardBlog_IsStillAllowed()
    {
        using var db = new TestDb();
        var blog = db.SeedBlog("Random", "random");

        var handler = new CreatePostCommandHandler(db.Db, db.FileReferenceService());
        var id = await handler.Handle(
            new CreatePostCommand(blog.Id, "Fine", "fine", "Body", null, "general", null, true),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task UpdatePost_IntoAProjectBlog_IsRejected()
    {
        using var db = new TestDb();
        var blog = db.SeedBlog("Game Dev Projects", "game-dev", "markdown", BlogKind.Project);
        var seeded = db.SeedProject(blog, "Live", "live", ProjectCategory.GameDev);

        var handler = new UpdatePostCommandHandler(db.Db, db.FileReferenceService());

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new UpdatePostCommand(blog.Id, seeded.Id, "Renamed", "renamed", "Body", null,
                                 "general", null, IsPublished: true, PublishedOn: null, CanonicalUrl: null),
            CancellationToken.None));
    }

    [Fact]
    public async Task UpdatePost_ForAMissingPost_StillReturnsFalse()
    {
        // The boundary check must not turn "post not found" into a throw.
        using var db = new TestDb();
        var blog = db.SeedBlog("Random", "random");

        var handler = new UpdatePostCommandHandler(db.Db, db.FileReferenceService());

        var result = await handler.Handle(
            new UpdatePostCommand(blog.Id, Guid.NewGuid(), "T", "t", "B", null,
                                 "general", null, IsPublished: true, PublishedOn: null, CanonicalUrl: null),
            CancellationToken.None);

        Assert.False(result);
    }
}
