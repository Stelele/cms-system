using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Posts;

public class GetPostBySlugQueryHandler(CmsDbContext db) : IQueryHandler<GetPostBySlugQuery, PostResponse?>
{
    public async Task<PostResponse?> Handle(GetPostBySlugQuery request, CancellationToken cancellationToken)
    {
        var post = await db.Posts
            .Include(p => p.Blog)
            .FirstOrDefaultAsync(p => p.Slug == request.Slug, cancellationToken);

        if (post == null) return null;

        return PostResponse.FromDomain(post);
    }
}
