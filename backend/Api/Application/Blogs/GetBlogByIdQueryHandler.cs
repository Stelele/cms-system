using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Blogs;

public class GetBlogByIdQueryHandler(CmsDbContext db) : IQueryHandler<GetBlogByIdQuery, BlogResponse?>
{
    public async Task<BlogResponse?> Handle(GetBlogByIdQuery request, CancellationToken cancellationToken)
    {
        var blog = await db.Blogs
            .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);

        if (blog == null) return null;

        return BlogResponse.FromDomain(blog);
    }
}
