using Domain.Blogs;
using System.Text.Json.Serialization;

namespace Application.DTOs;

public class BlogResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("createdOn")]
    public DateTimeOffset CreatedOn { get; init; }

    [JsonPropertyName("updatedOn")]
    public DateTimeOffset UpdatedOn { get; init; }

    public static BlogResponse FromDomain(Blog blog)
    {
        return new BlogResponse
        {
            Id = blog.Id,
            Name = blog.Name,
            Slug = blog.Slug,
            Description = blog.Description,
            CreatedOn = blog.CreatedOn,
            UpdatedOn = blog.UpdatedOn
        };
    }
}
