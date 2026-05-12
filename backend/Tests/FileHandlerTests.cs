using Application.Files;
using Domain.Files;
using Infrastructure.Models;
using Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Tests;

public class FileHandlerTests : IDisposable
{
    private readonly CmsDbContext _db;
    private readonly Mock<IR2StorageService> _r2Mock;
    private readonly Mock<IPublisher> _publisherMock;
    private static readonly byte[] SampleImage = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];

    public FileHandlerTests()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<CmsDbContext>()
            .UseSqlite(connection)
            .Options;

        _publisherMock = new Mock<IPublisher>();
        _db = new CmsDbContext(options, _publisherMock.Object);
        _db.Database.EnsureCreated();

        _r2Mock = new Mock<IR2StorageService>();
        _r2Mock.Setup(r => r.PublicBucket).Returns("cms-public");
        _r2Mock.Setup(r => r.PublicBucketUrl).Returns("https://pub-xxx.r2.dev");
        _r2Mock.Setup(r => r.Environment).Returns("local");
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private static IFormFile MakeFormFile(string name, string contentType, byte[] content)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
    }

    [Fact]
    public async Task UploadFile_NewFile_SavesToR2AndDb()
    {
        var handler = new UploadFileCommandHandler(_db, _r2Mock.Object);
        var file = MakeFormFile("photo.jpg", "image/jpeg", SampleImage);

        var result = await handler.Handle(new UploadFileCommand(file), default);

        Assert.True(result.IsNew);
        Assert.Equal("photo.jpg", result.FileName);
        Assert.StartsWith("https://pub-xxx.r2.dev/cms-app/local/images/", result.Url);
        Assert.EndsWith(".jpg", result.Url);

        _r2Mock.Verify(r => r.UploadAsync(
            "cms-public",
            It.Is<string>(k => k.StartsWith("cms-app/local/images/")),
            It.IsAny<Stream>(),
            "image/jpeg",
            default), Times.Once);

        var saved = await _db.FileItems.FirstOrDefaultAsync(f => f.Id == result.Id);
        Assert.NotNull(saved);
        Assert.Equal("photo.jpg", saved.FileName);
    }

    [Fact]
    public async Task UploadFile_DuplicateContent_ReturnsExisting()
    {
        var handler = new UploadFileCommandHandler(_db, _r2Mock.Object);
        var file = MakeFormFile("photo.jpg", "image/jpeg", SampleImage);

        var first = await handler.Handle(new UploadFileCommand(file), default);
        var second = await handler.Handle(new UploadFileCommand(file), default);

        Assert.False(second.IsNew);
        Assert.Equal(first.Id, second.Id);

        _r2Mock.Verify(r => r.UploadAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<Stream>(), It.IsAny<string>(), default), Times.Once);

        var count = await _db.FileItems.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task UploadFile_Gif_GoesToGifsFolder()
    {
        var handler = new UploadFileCommandHandler(_db, _r2Mock.Object);
        var file = MakeFormFile("animation.gif", "image/gif", SampleImage);

        var result = await handler.Handle(new UploadFileCommand(file), default);

        Assert.Contains("/gifs/", result.Url);
    }

    [Fact]
    public async Task UploadFile_Video_GoesToVideosFolder()
    {
        var handler = new UploadFileCommandHandler(_db, _r2Mock.Object);
        var file = MakeFormFile("clip.mp4", "video/mp4", SampleImage);

        var result = await handler.Handle(new UploadFileCommand(file), default);

        Assert.Contains("/videos/", result.Url);
    }

    [Fact]
    public async Task UploadFile_UnknownType_GoesToOtherFolder()
    {
        var handler = new UploadFileCommandHandler(_db, _r2Mock.Object);
        var file = MakeFormFile("doc.pdf", "application/pdf", SampleImage);

        var result = await handler.Handle(new UploadFileCommand(file), default);

        Assert.Contains("/other/", result.Url);
    }

    [Fact]
    public async Task DeleteFile_ExistingFile_RemovesFromR2AndDb()
    {
        var fileItem = new FileItem
        {
            Id = Guid.NewGuid(),
            FileName = "test.jpg",
            Extension = "jpg",
            ContentType = "image/jpeg",
            Size = 100,
            StoragePath = "cms-app/local/images/test.jpg",
            Url = "https://pub-xxx.r2.dev/cms-app/local/images/test.jpg",
            ContentHash = "HASH",
        };
        _db.FileItems.Add(fileItem);
        await _db.SaveChangesAsync();

        var handler = new DeleteFileCommandHandler(_db, _r2Mock.Object);
        var result = await handler.Handle(new DeleteFileCommand(fileItem.Id), default);

        Assert.True(result);

        _r2Mock.Verify(r => r.DeleteAsync("cms-public", fileItem.StoragePath, default), Times.Once);

        var deleted = await _db.FileItems.FindAsync(fileItem.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteFile_AssociatedWithPost_ReturnsFalse()
    {
        var blog = new Domain.Blogs.Blog
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Slug = "test",
            Description = "Test",
        };
        var post = new Domain.Posts.Post
        {
            Id = Guid.NewGuid(),
            BlogId = blog.Id,
            Title = "Test",
            Content = "Content",
            Slug = "test",
        };
        _db.Blogs.Add(blog);
        var fileItem = new FileItem
        {
            Id = Guid.NewGuid(),
            FileName = "test.jpg",
            Extension = "jpg",
            ContentType = "image/jpeg",
            Size = 100,
            StoragePath = "path",
            Url = "url",
            ContentHash = "HASH",
        };
        fileItem.Posts.Add(post);
        _db.Posts.Add(post);
        _db.FileItems.Add(fileItem);
        await _db.SaveChangesAsync();

        var handler = new DeleteFileCommandHandler(_db, _r2Mock.Object);
        var result = await handler.Handle(new DeleteFileCommand(fileItem.Id), default);

        Assert.False(result);
        _r2Mock.Verify(r => r.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task DeleteFile_NotFound_ReturnsFalse()
    {
        var handler = new DeleteFileCommandHandler(_db, _r2Mock.Object);
        var result = await handler.Handle(new DeleteFileCommand(Guid.NewGuid()), default);

        Assert.False(result);
    }

    [Fact]
    public async Task ReconcilePostFiles_AddsNewAssociation()
    {
        var blog = new Domain.Blogs.Blog { Id = Guid.NewGuid(), Name = "B", Slug = "b", Description = "D" };
        var post = new Domain.Posts.Post { Id = Guid.NewGuid(), BlogId = blog.Id, Title = "T", Content = "C", Slug = "s" };
        var file = new FileItem
        {
            Id = Guid.NewGuid(),
            Url = "https://pub-xxx.r2.dev/cms-app/local/images/test.jpg",
            FileName = "test.jpg",
            Extension = "jpg",
            ContentType = "image/jpeg",
            Size = 100,
            StoragePath = "path",
            ContentHash = "HASH",
        };
        _db.Blogs.Add(blog);
        _db.Posts.Add(post);
        _db.FileItems.Add(file);
        await _db.SaveChangesAsync();

        var service = new FileReferenceService(_db, _r2Mock.Object);
        await service.ReconcilePostFilesAsync(post.Id, "![img](https://pub-xxx.r2.dev/cms-app/local/images/test.jpg)", null, default);

        var postReloaded = await _db.Posts.Include(p => p.Files).FirstAsync(p => p.Id == post.Id);
        Assert.Single(postReloaded.Files);
        Assert.Equal(file.Id, postReloaded.Files.First().Id);
    }

    [Fact]
    public async Task ReconcilePostFiles_RemovesStaleAssociation_AndMarksForDeletion()
    {
        var blog = new Domain.Blogs.Blog { Id = Guid.NewGuid(), Name = "B", Slug = "b", Description = "D" };
        var post = new Domain.Posts.Post { Id = Guid.NewGuid(), BlogId = blog.Id, Title = "T", Content = "C", Slug = "s" };
        var file = new FileItem
        {
            Id = Guid.NewGuid(),
            Url = "https://pub-xxx.r2.dev/cms-app/local/images/test.jpg",
            FileName = "test.jpg",
            Extension = "jpg",
            ContentType = "image/jpeg",
            Size = 100,
            StoragePath = "path",
            ContentHash = "HASH",
        };
        file.Posts.Add(post);
        _db.Blogs.Add(blog);
        _db.Posts.Add(post);
        _db.FileItems.Add(file);
        await _db.SaveChangesAsync();

        var service = new FileReferenceService(_db, _r2Mock.Object);
        await service.ReconcilePostFilesAsync(post.Id, "content with no images", null, default);

        var fileReloaded = await _db.FileItems.Include(f => f.Posts).FirstAsync(f => f.Id == file.Id);
        Assert.Empty(fileReloaded.Posts);
        Assert.NotNull(fileReloaded.MarkedForDeletionAt);
    }

    [Fact]
    public async Task ReconcilePostFiles_ReReferencedFile_ClearsDeletionMark()
    {
        var blog = new Domain.Blogs.Blog { Id = Guid.NewGuid(), Name = "B", Slug = "b", Description = "D" };
        var post = new Domain.Posts.Post { Id = Guid.NewGuid(), BlogId = blog.Id, Title = "T", Content = "C", Slug = "s" };
        var file = new FileItem
        {
            Id = Guid.NewGuid(),
            Url = "https://pub-xxx.r2.dev/cms-app/local/images/test.jpg",
            FileName = "test.jpg",
            Extension = "jpg",
            ContentType = "image/jpeg",
            Size = 100,
            StoragePath = "path",
            ContentHash = "HASH",
            MarkedForDeletionAt = DateTimeOffset.UtcNow.AddHours(1),
        };
        _db.Blogs.Add(blog);
        _db.Posts.Add(post);
        _db.FileItems.Add(file);
        await _db.SaveChangesAsync();

        var service = new FileReferenceService(_db, _r2Mock.Object);
        await service.ReconcilePostFilesAsync(post.Id, "![img](https://pub-xxx.r2.dev/cms-app/local/images/test.jpg)", null, default);

        var fileReloaded = await _db.FileItems.FirstAsync(f => f.Id == file.Id);
        Assert.Null(fileReloaded.MarkedForDeletionAt);
    }
}

public class ImageUrlExtractorTests
{
    private const string PublicBucketUrl = "https://pub-xxx.r2.dev";

    [Fact]
    public void ExtractImageUrls_MarkdownImages_ReturnsUrls()
    {
        var content = "Text ![alt](https://pub-xxx.r2.dev/cms-app/local/images/a.jpg) more ![alt2](https://pub-xxx.r2.dev/cms-app/local/images/b.png)";
        var result = ImageUrlExtractor.ExtractImageUrls(content, null, PublicBucketUrl);
        Assert.Equal(2, result.Count);
        Assert.Contains("https://pub-xxx.r2.dev/cms-app/local/images/a.jpg", result);
        Assert.Contains("https://pub-xxx.r2.dev/cms-app/local/images/b.png", result);
    }

    [Fact]
    public void ExtractImageUrls_ExternalUrls_Ignored()
    {
        var content = "Text ![alt](https://external.com/img.jpg)";
        var result = ImageUrlExtractor.ExtractImageUrls(content, null, PublicBucketUrl);
        Assert.Empty(result);
    }

    [Fact]
    public void ExtractImageUrls_HtmlImages_ReturnsUrls()
    {
        var content = "<img src=\"https://pub-xxx.r2.dev/cms-app/local/images/a.jpg\" />";
        var result = ImageUrlExtractor.ExtractImageUrls(content, null, PublicBucketUrl);
        Assert.Single(result);
    }

    [Fact]
    public void ExtractImageUrls_CoverImageUrl_ReturnsUrl()
    {
        var result = ImageUrlExtractor.ExtractImageUrls(null, "https://pub-xxx.r2.dev/cms-app/local/images/cover.jpg", PublicBucketUrl);
        Assert.Single(result);
    }

    [Fact]
    public void ExtractImageUrls_NullContent_ReturnsEmpty()
    {
        var result = ImageUrlExtractor.ExtractImageUrls(null, null, PublicBucketUrl);
        Assert.Empty(result);
    }
}
