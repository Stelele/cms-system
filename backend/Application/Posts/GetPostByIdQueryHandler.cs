using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Posts;

public class GetPostByIdQueryHandler(CmsDbContext db) : IQueryHandler<GetPostByIdQuery, PostResponse?>
{
    public async Task<PostResponse?> Handle(GetPostByIdQuery request, CancellationToken cancellationToken)
    {
        var post = await db.Posts
            .FirstOrDefaultAsync(p => p.BlogId == request.BlogId && p.Id == request.Id, cancellationToken);

        if (post == null) return null;

        return PostResponse.FromDomain(post);
    }
}
