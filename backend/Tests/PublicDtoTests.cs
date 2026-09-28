using Application.DTOs;
using Domain.Blogs;
using Domain.Posts;

namespace Tests;

public class PublicDtoTests
{
    [Fact]
    public void PublicBlogResponse_ResolvesNullContentTypeToMarkdown()
    {
        var blog = Blog.Create("Test", "test", "desc", "i-heroicons-book-open");

        Assert.Equal("markdown", PublicBlogResponse.FromDomain(blog).ContentType);
    }

    [Fact]
    public void PublicBlogResponse_PreservesHtml()
    {
        var blog = Blog.Create("Archive", "archive", "desc", "i-heroicons-book-open", "html");

        Assert.Equal("html", PublicBlogResponse.FromDomain(blog).ContentType);
    }

    [Fact]
    public void PublicPostResponse_ExposesNoInternalFields()
    {
        var exposed = typeof(PublicPostResponse)
            .GetProperties()
            .Select(p => p.Name)
            .ToHashSet();

        Assert.DoesNotContain("IsPublished", exposed);
        Assert.DoesNotContain("CreatedOn", exposed);
        Assert.DoesNotContain("UpdatedOn", exposed);
    }

    [Fact]
    public void PublicBlogResponse_ExposesNoInternalFields()
    {
        var exposed = typeof(PublicBlogResponse)
            .GetProperties()
            .Select(p => p.Name)
            .ToHashSet();

        Assert.DoesNotContain("CreatedOn", exposed);
        Assert.DoesNotContain("UpdatedOn", exposed);
    }

    [Fact]
    public void PublicPostResponse_FromDomain_CarriesEveryPublicField()
    {
        var blogId = Guid.NewGuid();
        var publishedOn = new DateTimeOffset(2024, 5, 17, 9, 30, 0, TimeSpan.Zero);
        var post = Post.Create(
            blogId,
            "A Title",
            "a-title",
            "<p>Body</p>",
            "A description",
            "tech",
            "https://example.com/cover.png",
            "https://original.example.com/post");
        post.SetPublishedOn(publishedOn);

        var dto = PublicPostResponse.FromDomain(post);

        Assert.Equal(post.Id, dto.Id);
        Assert.Equal(blogId, dto.BlogId);
        Assert.Equal("A Title", dto.Title);
        Assert.Equal("a-title", dto.Slug);
        Assert.Equal("<p>Body</p>", dto.Content);
        Assert.Equal("A description", dto.Description);
        Assert.Equal("tech", dto.Tag);
        Assert.Equal("https://example.com/cover.png", dto.CoverImageUrl);
        Assert.Equal(publishedOn, dto.PublishedOn);
        Assert.Equal("https://original.example.com/post", dto.CanonicalUrl);
    }
}
