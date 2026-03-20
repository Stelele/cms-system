using Domain.Abstractions;
using Domain.Posts;

namespace Domain.Blogs;

public class Blog : Base
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public List<Post> Posts { get; set; } = [];

    public static Blog Create(
        string name,
        string slug,
        string description)
    {
        return new Blog
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            Description = description
        };
    }
}
