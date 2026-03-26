using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Posts;

public class GetPostsByBlogQueryHandler(CmsDbContext db) : IQueryHandler<GetPostsByBlogQuery, List<PostResponse>>
{
    public async Task<List<PostResponse>> Handle(GetPostsByBlogQuery request, CancellationToken cancellationToken)
    {
        var query = db.Posts.Where(p => p.BlogId == request.BlogId);

        if (!string.IsNullOrEmpty(request.Tag))
            query = query.Where(p => p.Tag == request.Tag);

        if (request.IsPublished.HasValue)
            query = query.Where(p => p.IsPublished == request.IsPublished.Value);

        var posts = await query.ToListAsync(cancellationToken);

        return posts
            .OrderByDescending(p => p.UpdatedOn)
            .Select(PostResponse.FromDomain)
            .ToList();
    }
}
