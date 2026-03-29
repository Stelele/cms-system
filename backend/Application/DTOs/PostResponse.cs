using Domain.Posts;
using System.Text.Json.Serialization;

namespace Application.DTOs;

public class PostResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("blogId")]
    public Guid BlogId { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; init; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("tag")]
    public string Tag { get; init; } = string.Empty;

    [JsonPropertyName("coverImageUrl")]
    public string? CoverImageUrl { get; init; }

    [JsonPropertyName("publishedOn")]
    public DateTimeOffset? PublishedOn { get; init; }

    [JsonPropertyName("isPublished")]
    public bool IsPublished { get; init; }

    [JsonPropertyName("createdOn")]
    public DateTimeOffset CreatedOn { get; init; }

    [JsonPropertyName("updatedOn")]
    public DateTimeOffset UpdatedOn { get; init; }

    public static PostResponse FromDomain(Post post)
    {
        return new PostResponse
        {
            Id = post.Id,
            BlogId = post.BlogId,
            Title = post.Title,
            Slug = post.Slug,
            Content = post.Content,
            Description = post.Description,
            Tag = post.Tag,
            CoverImageUrl = post.CoverImageUrl,
            PublishedOn = post.PublishedOn,
            IsPublished = post.IsPublished,
            CreatedOn = post.CreatedOn,
            UpdatedOn = post.UpdatedOn
        };
    }
}
