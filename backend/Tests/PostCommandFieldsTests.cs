using Application.Posts;
using System.Text.Json;

namespace Tests;

public class PostCommandFieldsTests
{
    [Fact]
    public async Task Create_WithPublishedOn_StoresTheSuppliedDate()
    {
        using var db = new TestDb();
        var blog = db.SeedBlog("Archive", "archive", "html");
        var when = new DateTimeOffset(2023, 4, 1, 0, 0, 0, TimeSpan.Zero);

        var handler = new CreatePostCommandHandler(db.Db, db.FileReferenceService());
        var id = await handler.Handle(
            new CreatePostCommand(blog.Id, "A Post", "a-post", "Body", "Brief",
                                 "general", null, IsPublished: true, PublishedOn: when),
            CancellationToken.None);

        db.Db.ChangeTracker.Clear();
        var post = db.Db.Posts.Single(p => p.Id == id);
        Assert.True(post.IsPublished);
        Assert.Equal(when, post.PublishedOn);
    }

    [Fact]
    public async Task Create_WithoutPublishedOn_DefaultsToNow()
    {
        using var db = new TestDb();
        var blog = db.SeedBlog("Archive", "archive", "html");
        var before = DateTimeOffset.UtcNow.AddSeconds(-5);

        var handler = new CreatePostCommandHandler(db.Db, db.FileReferenceService());
        var id = await handler.Handle(
            new CreatePostCommand(blog.Id, "A Post", "a-post", "Body", "Brief",
                                 "general", null, IsPublished: true, PublishedOn: null),
            CancellationToken.None);

        db.Db.ChangeTracker.Clear();
        var post = db.Db.Posts.Single(p => p.Id == id);
        Assert.NotNull(post.PublishedOn);
        Assert.True(post.PublishedOn > before);
    }

    [Fact]
    public async Task Create_WithCanonicalUrl_StoresIt()
    {
        using var db = new TestDb();
        var blog = db.SeedBlog("Archive", "archive", "html");
        const string canonical = "https://hashnode.dev/@gift/post";

        var handler = new CreatePostCommandHandler(db.Db, db.FileReferenceService());
        var id = await handler.Handle(
            new CreatePostCommand(blog.Id, "A Post", "a-post", "Body", "Brief",
                                 "general", null, IsPublished: true,
                                 PublishedOn: null, CanonicalUrl: canonical),
            CancellationToken.None);

        db.Db.ChangeTracker.Clear();
        Assert.Equal(canonical, db.Db.Posts.Single(p => p.Id == id).CanonicalUrl);
    }

    [Fact]
    public async Task Update_WithPublishedOn_OverwritesThePublishDate()
    {
        using var db = new TestDb();
        var blog = db.SeedBlog("Archive", "archive", "html");
        var seeded = db.SeedPost(blog, "Old", "old", isPublished: true);
        var when = new DateTimeOffset(2020, 1, 15, 0, 0, 0, TimeSpan.Zero);

        var handler = new UpdatePostCommandHandler(db.Db, db.FileReferenceService());
        var ok = await handler.Handle(
            new UpdatePostCommand(blog.Id, seeded.Id, "Old", "old", "Body", "Brief",
                                 "general", null, IsPublished: true,
                                 PublishedOn: when, CanonicalUrl: null),
            CancellationToken.None);

        Assert.True(ok);
        db.Db.ChangeTracker.Clear();
        Assert.Equal(when, db.Db.Posts.Single(p => p.Id == seeded.Id).PublishedOn);
    }

    [Fact]
    public void CreatePostCommandValidator_RejectsCanonicalUrlThatIsNotHttp()
    {
        var command = new CreatePostCommand(Guid.NewGuid(), "T", "t", "Body", null,
                                            "general", null, true, null, "javascript:alert(1)");

        var failures = new CreatePostCommandValidator().Validate(command).Errors;

        Assert.Contains(failures, f => f.PropertyName == nameof(CreatePostCommand.CanonicalUrl));
    }

    [Fact]
    public async Task Update_WithCanonicalUrl_WritesItThrough()
    {
        using var db = new TestDb();
        var blog = db.SeedBlog("Archive", "archive", "html");
        var seeded = db.SeedPost(blog, "Old", "old", isPublished: true);
        const string canonical = "https://medium.com/@gift/old";

        var handler = new UpdatePostCommandHandler(db.Db, db.FileReferenceService());
        var ok = await handler.Handle(
            new UpdatePostCommand(blog.Id, seeded.Id, "Old", "old", "Body", "Brief",
                                 "general", null, IsPublished: true,
                                 PublishedOn: null, CanonicalUrl: canonical),
            CancellationToken.None);

        Assert.True(ok);
        db.Db.ChangeTracker.Clear();
        Assert.Equal(canonical, db.Db.Posts.Single(p => p.Id == seeded.Id).CanonicalUrl);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com/file")]
    public void UpdatePostCommandValidator_RejectsCanonicalUrlThatIsNotHttp(string invalid)
    {
        var command = new UpdatePostCommand(
            Guid.NewGuid(), Guid.NewGuid(), "T", "t", "Body", null,
            "general", null, IsPublished: true, PublishedOn: null, CanonicalUrl: invalid);

        var failures = new UpdatePostCommandValidator().Validate(command).Errors;

        Assert.Contains(failures, f => f.PropertyName == nameof(UpdatePostCommand.CanonicalUrl));
    }

    [Fact]
    public void UpdatePostCommandValidator_AcceptsNullCanonicalUrl()
    {
        var command = new UpdatePostCommand(
            Guid.NewGuid(), Guid.NewGuid(), "T", "t", "Body", null,
            "general", null, IsPublished: true, PublishedOn: null, CanonicalUrl: null);

        Assert.Empty(new UpdatePostCommandValidator().Validate(command).Errors);
    }

    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private const string CreateWithoutIsPublished =
        """{"blogId":"11111111-1111-1111-1111-111111111111","title":"T","slug":"t","content":"C","tag":"g","coverImageUrl":null}""";

    private const string CreateComplete =
        """{"blogId":"11111111-1111-1111-1111-111111111111","title":"T","slug":"t","content":"C","tag":"g","coverImageUrl":null,"isPublished":true}""";

    [Fact]
    public void CreatePostCommand_MissingIsPublished_Throws()
    {
        // Without [JsonRequired] this deserialised to IsPublished=false and
        // silently created a draft the caller never asked for.
        var ex = Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<CreatePostCommand>(CreateWithoutIsPublished, Web));

        Assert.Contains("isPublished", ex.Message);
    }

    [Fact]
    public void CreatePostCommand_CompletePayload_Deserialises()
    {
        var command = JsonSerializer.Deserialize<CreatePostCommand>(CreateComplete, Web);

        Assert.True(command!.IsPublished);
        Assert.Equal("T", command.Title);
    }

    [Fact]
    public void CreatePostCommand_MissingTitle_Throws()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CreatePostCommand>(
            """{"blogId":"11111111-1111-1111-1111-111111111111","slug":"t","content":"C","tag":"g","coverImageUrl":null,"isPublished":true}""",
            Web));
    }

    [Fact]
    public void CreatePostCommand_MissingCoverImageUrl_Throws()
    {
        // coverImageUrl was `required` but nullable: omitting it is an error,
        // while an explicit null is fine.
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CreatePostCommand>(
            """{"blogId":"11111111-1111-1111-1111-111111111111","title":"T","slug":"t","content":"C","tag":"g","isPublished":true}""",
            Web));
    }

    [Fact]
    public void UpdatePostCommand_MissingIsPublished_Throws()
    {
        var ex = Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<UpdatePostCommand>(
                """{"blogId":"11111111-1111-1111-1111-111111111111","id":"22222222-2222-2222-2222-222222222222","title":"T","slug":"t","content":"C","tag":"g","coverImageUrl":null}""",
                Web));

        Assert.Contains("isPublished", ex.Message);
    }

    [Fact]
    public void UpdatePostCommand_CompletePayload_Deserialises()
    {
        var command = JsonSerializer.Deserialize<UpdatePostCommand>(
            """{"blogId":"11111111-1111-1111-1111-111111111111","id":"22222222-2222-2222-2222-222222222222","title":"T","slug":"t","content":"C","tag":"g","coverImageUrl":null,"isPublished":false}""",
            Web);

        Assert.False(command!.IsPublished);
        Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222222"), command.Id);
    }

    [Fact]
    public void PostCommands_OptionalNewFields_MayBeOmitted()
    {
        var create = JsonSerializer.Deserialize<CreatePostCommand>(CreateComplete, Web);
        var update = JsonSerializer.Deserialize<UpdatePostCommand>(
            """{"blogId":"11111111-1111-1111-1111-111111111111","id":"22222222-2222-2222-2222-222222222222","title":"T","slug":"t","content":"C","tag":"g","coverImageUrl":null,"isPublished":true}""",
            Web);

        Assert.Null(create!.PublishedOn);
        Assert.Null(create.CanonicalUrl);
        Assert.Null(update!.PublishedOn);
        Assert.Null(update.CanonicalUrl);
    }
}
