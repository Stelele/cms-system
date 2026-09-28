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

    [Theory]
    [InlineData(null, BlogContentType.Markdown)]
    [InlineData("", BlogContentType.Markdown)]
    [InlineData("   ", BlogContentType.Markdown)]
    [InlineData("bogus", BlogContentType.Markdown)]
    [InlineData("html", BlogContentType.Html)]
    [InlineData("markdown", BlogContentType.Markdown)]
    public void OrDefault_MapsEveryInputToALegalValue(string? input, string expected)
    {
        Assert.Equal(expected, BlogContentType.OrDefault(input));
    }

    [Fact]
    public void ContentType_RoundTripsThroughTheDatabase()
    {
        using var db = new TestDb();
        db.SeedBlog("Graphics", "graphics", BlogContentType.Html);

        // Without this, Single() returns the tracked instance just saved and the
        // assertion never reaches SQLite.
        db.Db.ChangeTracker.Clear();

        Assert.Equal(BlogContentType.Html, db.Db.Blogs.Single().ContentType);
    }
}
