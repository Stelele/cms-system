using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Posts;

public record GetPublicPostsByBlogQuery(Guid BlogId) : IQuery<List<PublicPostResponse>>;

public class GetPublicPostsByBlogQueryHandler(CmsDbContext db)
    : IQueryHandler<GetPublicPostsByBlogQuery, List<PublicPostResponse>>
{
    public async Task<List<PublicPostResponse>> Handle(
        GetPublicPostsByBlogQuery request,
        CancellationToken cancellationToken)
    {
        // IsPublished is a constant in the Where clause, not a parameter. A
        // caller cannot opt out of it.
        var posts = await db.Posts
            .Where(p => p.BlogId == request.BlogId && p.IsPublished)
            .OrderByDescending(p => p.PublishedOn)
            .ToListAsync(cancellationToken);

        return posts.Select(PublicPostResponse.FromDomain).ToList();
    }
}
