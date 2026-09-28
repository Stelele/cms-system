using Domain.Posts;

namespace Application.DTOs;

/// <summary>
/// The anonymous projection of a post. Only ever produced by a query that
/// filters IsPublished, so reaching a draft requires a code change rather than a
/// missed parameter.
/// </summary>
public record PublicPostResponse(
    Guid Id,
    Guid BlogId,
    string Title,
    string Slug,
    string Content,
    string? Description,
    string Tag,
    string? CoverImageUrl,
    DateTimeOffset? PublishedOn,
    string? CanonicalUrl
)
{
    public static PublicPostResponse FromDomain(Post post) =>
        new(post.Id,
            post.BlogId,
            post.Title,
            post.Slug,
            post.Content,
            post.Description,
            post.Tag,
            post.CoverImageUrl,
            post.PublishedOn,
            post.CanonicalUrl);
}
