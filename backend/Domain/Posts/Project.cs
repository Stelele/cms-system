using Domain.Blogs;

namespace Domain.Posts;

/// <summary>
/// A project is a post with typed project fields. Everything about writing and
/// publishing is inherited; only the structured bits are added. EF maps this as
/// Table-Per-Hierarchy, so there is one Posts table with a discriminator.
/// </summary>
public class Project : Post
{
    /// <summary>
    /// Denormalised from the containing blog and validated against it, so
    /// ProjectResponse stays a flat read DTO with no per-row join.
    /// </summary>
    public ProjectCategory Category { get; set; }

    public List<string> Stack { get; set; } = [];

    public int Year { get; set; }

    /// <summary>Last push to the backing repository. The input to the derived status.</summary>
    public DateTimeOffset? LastPushedAt { get; set; }

    public List<ProjectLink> Links { get; set; } = [];

    public static Project Create(
        Guid blogId,
        string title,
        string slug,
        string content,
        string? description,
        ProjectCategory category,
        int year)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            BlogId = blogId,
            Title = title,
            Slug = slug,
            Content = content,
            Description = description,
            Year = year,
            Category = category,
        };

        // Post.Tag is NOT NULL and has no project-specific meaning. The category
        // slug is the closest honest value; nothing lets a caller set it, so it
        // cannot contradict Category.
        project.Tag = ProjectBlogs.SlugFor(category);
        return project;
    }
}
