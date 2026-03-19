using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Posts;

public class GetTagsQueryHandler(CmsDbContext db) : IQueryHandler<GetTagsQuery, List<TagResponse>>
{
    public async Task<List<TagResponse>> Handle(GetTagsQuery request, CancellationToken cancellationToken)
    {
        var tags = await db.Posts
            .Where(p => p.IsPublished)
            .GroupBy(p => p.Tag)
            .Select(g => new TagResponse
            {
                Tag = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(t => t.Count)
            .ToListAsync(cancellationToken);

        return tags;
    }
}
