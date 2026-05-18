using Domain.Blogs;
using System.Text.Json.Serialization;

namespace Application.DTOs;

public record BlogResponse(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    string Icon,
    DateTimeOffset CreatedOn,
    DateTimeOffset UpdatedOn
)
{
    public static BlogResponse FromDomain(Blog blog) =>
        new(blog.Id, blog.Name, blog.Slug, blog.Description, blog.Icon, blog.CreatedOn, blog.UpdatedOn);
}