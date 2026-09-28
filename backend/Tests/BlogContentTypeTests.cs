using Domain.Blogs;

namespace Tests;

public class BlogContentTypeTests
{
    [Fact]
    public void Create_WithoutContentType_StoresNull()
    {
        var blog = Blog.Create("Test", "test", "desc", "i-heroicons-book-open");

        Assert.Null(blog.ContentType);
    }

    [Fact]
    public void Create_WithHtml_StoresHtml()
    {
        var blog = Blog.Create("Archive", "archive", "desc", "i-heroicons-book-open", BlogContentType.Html);

        Assert.Equal(BlogContentType.Html, blog.ContentType);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("MARKDOWN")]
    [InlineData("restructuredtext")]
    public void Create_WithInvalidContentType_Throws(string invalid)
    {
        Assert.Throws<ArgumentException>(
            () => Blog.Create("Test", "test", "desc", "i-heroicons-book-open", invalid));
    }

    [Fact]
    public void OrDefault_NullYieldsMarkdown()
    {
        Assert.Equal(BlogContentType.Markdown, BlogContentType.OrDefault(null));
    }

    [Fact]
    public void ContentType_RoundTripsThroughTheDatabase()
    {
        using var db = new TestDb();
        db.SeedBlog("Graphics", "graphics", BlogContentType.Html);

        Assert.Equal(BlogContentType.Html, db.Db.Blogs.Single().ContentType);
    }
}
