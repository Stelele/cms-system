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
        string? coverImageUrl = null)
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
            CoverImageUrl = coverImageUrl
        };
    }

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
