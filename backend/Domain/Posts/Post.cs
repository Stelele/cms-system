using Domain.Abstractions;
using Domain.Blogs;
using Domain.Files;

namespace Domain.Posts;

public class Post : Base
{
    public Guid BlogId { get; set; }
    public Blog Blog { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Tag { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }

    /// <summary>
    /// The post's original location, when it was imported from somewhere else.
    /// Drives the SEO canonical link and the "view original" affordance.
    /// </summary>
    public string? CanonicalUrl { get; set; }

    public DateTimeOffset? PublishedOn { get; set; }
    public bool IsPublished { get; set; }

    public ICollection<FileItem> Files { get; set; } = [];

    public static Post Create(
        Guid blogId,
        string title,
        string slug,
        string content,
        string? description,
        string tag,
        string? coverImageUrl = null,
        string? canonicalUrl = null)
    {
        return new Post
        {
            Id = Guid.NewGuid(),
            BlogId = blogId,
            Title = title,
            Slug = slug,
            Content = content,
            Description = description,
            Tag = tag,
            CoverImageUrl = coverImageUrl,
            CanonicalUrl = canonicalUrl
        };
    }

    /// <summary>
    /// Overrides the publish date. <see cref="Publish"/> always stamps UtcNow, so
    /// importing an archive has to be able to correct it afterwards.
    /// </summary>
    public void SetPublishedOn(DateTimeOffset publishedOn) => PublishedOn = publishedOn;

    public void Publish()
    {
        IsPublished = true;
        PublishedOn = DateTimeOffset.UtcNow;
    }

    public void Unpublish()
    {
        IsPublished = false;
    }
}
