using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Blogs;

public class GetBlogsQueryHandler(CmsDbContext db) : IQueryHandler<GetBlogsQuery, List<BlogResponse>>
{
    public async Task<List<BlogResponse>> Handle(GetBlogsQuery request, CancellationToken cancellationToken)
    {
        var blogs = await db.Blogs
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);

        return blogs.Select(BlogResponse.FromDomain).ToList();
    }
}
