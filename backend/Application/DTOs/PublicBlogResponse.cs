using Domain.Blogs;

namespace Application.DTOs;

/// <summary>
/// The anonymous projection of a blog. Deliberately omits CreatedOn and
/// UpdatedOn: a field that is not on the record cannot be serialised, which is
/// the only durable way to keep internal bookkeeping off a public endpoint.
/// </summary>
public record PublicBlogResponse(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    string Icon,
    string ContentType
)
{
    public static PublicBlogResponse FromDomain(Blog blog) =>
        new(blog.Id,
            blog.Name,
            blog.Slug,
            blog.Description,
            blog.Icon,
            BlogContentType.OrDefault(blog.ContentType));
}
