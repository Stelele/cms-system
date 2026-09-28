using Application.Blogs;
using Application.Projects;
using Domain.Blogs;
using Domain.Posts;

namespace Tests;

/// <summary>
/// The admin project read must show drafts, because that is the whole point of
/// the admin UI, and the public project read must not - because the drafts are
/// half-written articles. These are two separate queries on purpose: the
/// published-only guarantee lives in the public one so that loosening the admin
/// read cannot weaken it.
/// </summary>
public class ProjectDraftVisibilityTests
{
    [Fact]
    public async Task AdminList_IncludesDrafts()
    {
        using var db = new TestDb();
        var blog = db.SeedBlog("Game Dev Projects", "game-dev", null, BlogKind.Project);
        db.SeedProject(blog, "Live", "live", ProjectCategory.GameDev, isPublished: true);
        db.SeedProject(blog, "Draft", "draft", ProjectCategory.GameDev, isPublished: false);

        var result = await new GetProjectsQueryHandler(db.Db)
            .Handle(new GetProjectsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Slug == "draft");
    }

    [Fact]
    public async Task AdminList_StillExcludesOrdinaryPosts()
    {
        using var db = new TestDb();
        var blog = db.SeedBlog("Game Dev Projects", "game-dev", null, BlogKind.Project);
        db.SeedPost(blog, "Just a post", "just-a-post");
        db.SeedProject(blog, "A project", "a-project", ProjectCategory.GameDev, isPublished: false);

        var result = await new GetProjectsQueryHandler(db.Db)
            .Handle(new GetProjectsQuery(), CancellationToken.None);

        var only = Assert.Single(result);
        Assert.Equal("a-project", only.Slug);
    }

    [Fact]
    public async Task PublicList_ExcludesDrafts()
    {
        using var db = new TestDb();
        var blog = db.SeedBlog("Game Dev Projects", "game-dev", null, BlogKind.Project);
        db.SeedProject(blog, "Live", "live", ProjectCategory.GameDev, isPublished: true);
        db.SeedProject(blog, "Draft", "draft", ProjectCategory.GameDev, isPublished: false);

        var result = await new GetPublicProjectsQueryHandler(db.Db)
            .Handle(new GetPublicProjectsQuery(), CancellationToken.None);

        var only = Assert.Single(result);
        Assert.Equal("live", only.Slug);
    }

    [Fact]
    public async Task PublicBySlug_ReturnsNullForADraft()
    {
        using var db = new TestDb();
        var blog = db.SeedBlog("Game Dev Projects", "game-dev", null, BlogKind.Project);
        db.SeedProject(blog, "Draft", "draft", ProjectCategory.GameDev, isPublished: false);

        var result = await new GetPublicProjectBySlugQueryHandler(db.Db)
            .Handle(new GetPublicProjectBySlugQuery("draft"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task AdminBySlug_ReturnsADraft()
    {
        using var db = new TestDb();
        var blog = db.SeedBlog("Game Dev Projects", "game-dev", null, BlogKind.Project);
        db.SeedProject(blog, "Draft", "draft", ProjectCategory.GameDev, isPublished: false);

        var result = await new GetProjectBySlugQueryHandler(db.Db)
            .Handle(new GetProjectBySlugQuery("draft"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("draft", result!.Slug);
    }

    [Fact]
    public async Task AdminList_FiltersByCategory()
    {
        using var db = new TestDb();
        var gameDev = db.SeedBlog("Game Dev Projects", "game-dev", null, BlogKind.Project);
        var graphics = db.SeedBlog("Graphics Projects", "graphics", null, BlogKind.Project);
        db.SeedProject(gameDev, "A game", "a-game", ProjectCategory.GameDev, isPublished: false);
        db.SeedProject(graphics, "A shader", "a-shader", ProjectCategory.Graphics, isPublished: false);

        var result = await new GetProjectsQueryHandler(db.Db)
            .Handle(new GetProjectsQuery(ProjectCategory.Graphics), CancellationToken.None);

        var only = Assert.Single(result);
        Assert.Equal("a-shader", only.Slug);
    }
}
