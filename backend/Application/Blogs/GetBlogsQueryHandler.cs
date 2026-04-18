using Application.Abstractions;
using Application.DTOs;
using Domain.Blogs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Blogs;

public class GetBlogsQueryHandler(CmsDbContext db) : IQueryHandler<GetBlogsQuery, List<BlogResponse>>
{
    public async Task<List<BlogResponse>> Handle(GetBlogsQuery request, CancellationToken cancellationToken)
    {
        var blogs = db.Blogs
            .OrderBy(b => b.Name);

        List<Blog> blogsList = [];
        if (request.Slugs is not null)
        {
            blogsList = await blogs
                .Where(b => request.Slugs.Contains(b.Slug)).ToListAsync(cancellationToken);
        }
        else 
        {
            blogsList = await blogs.ToListAsync(cancellationToken);
        }


        return blogsList.Select(BlogResponse.FromDomain).ToList();
    }
}
