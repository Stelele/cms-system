using Domain.Posts;
using System.Text.Json.Serialization;

namespace Application.DTOs;

public record PostResponse(
    Guid Id,
    Guid BlogId,
    string Title,
    string Slug,
    string Content,
    string? Description,
    string Tag,
    string? CoverImageUrl,
    DateTimeOffset? PublishedOn,
    bool IsPublished,
    DateTimeOffset CreatedOn,
    DateTimeOffset UpdatedOn
)
{
    public static PostResponse FromDomain(Post post) =>
        new(post.Id, post.BlogId, post.Title, post.Slug, post.Content, post.Description, post.Tag, post.CoverImageUrl, post.PublishedOn, post.IsPublished, post.CreatedOn, post.UpdatedOn);
}
