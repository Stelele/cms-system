using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Posts;

public record GetPublicPostBySlugQuery(string Slug) : IQuery<PublicPostResponse?>;

public class GetPublicPostBySlugQueryHandler(CmsDbContext db)
    : IQueryHandler<GetPublicPostBySlugQuery, PublicPostResponse?>
{
    public async Task<PublicPostResponse?> Handle(
        GetPublicPostBySlugQuery request,
        CancellationToken cancellationToken)
    {
        var post = await db.Posts
            .Where(p => p.Slug == request.Slug && p.IsPublished)
            .FirstOrDefaultAsync(cancellationToken);

        return post is null ? null : PublicPostResponse.FromDomain(post);
    }
}
