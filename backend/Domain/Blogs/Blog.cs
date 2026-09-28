using Domain.Abstractions;
using Domain.Posts;

namespace Domain.Blogs;

public class Blog : Base
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "i-heroicons-book-open";

    /// <summary>
    /// One of <see cref="BlogContentType"/>. Null means markdown, which is what
    /// every pre-existing blog already is.
    /// </summary>
    public string? ContentType { get; set; }

    public List<Post> Posts { get; set; } = [];

    public static Blog Create(
        string name,
        string slug,
        string description,
        string icon,
        string? contentType = null)
    {
        if (!BlogContentType.IsValid(contentType))
            throw new ArgumentException(
                $"'{contentType}' is not a valid content type.", nameof(contentType));

        return new Blog
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            Description = description,
            Icon = icon,
            ContentType = contentType
        };
    }
}
