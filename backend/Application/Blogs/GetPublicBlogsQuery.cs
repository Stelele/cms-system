using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Blogs;

public record GetPublicBlogsQuery(string[]? Slugs = null) : IQuery<List<PublicBlogResponse>>;

public class GetPublicBlogsQueryHandler(CmsDbContext db) : IQueryHandler<GetPublicBlogsQuery, List<PublicBlogResponse>>
{
    public async Task<List<PublicBlogResponse>> Handle(
        GetPublicBlogsQuery request,
        CancellationToken cancellationToken)
    {
        var query = db.Blogs.AsQueryable();

        if (request.Slugs is { Length: > 0 })
            query = query.Where(b => request.Slugs.Contains(b.Slug));

        var blogs = await query
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);

        return blogs.Select(PublicBlogResponse.FromDomain).ToList();
    }
}
