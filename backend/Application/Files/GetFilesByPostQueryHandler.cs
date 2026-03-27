using Application.Abstractions;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Files;

public class GetFilesByPostQueryHandler(CmsDbContext db) : IQueryHandler<GetFilesByPostQuery, List<FileResponse>>
{
    public async Task<List<FileResponse>> Handle(GetFilesByPostQuery request, CancellationToken cancellationToken)
    {
        var files = await db.FileItems
            .Where(f => f.Posts.Any(p => p.Id == request.PostId))
            .ToListAsync(cancellationToken);

        return files.Select(f => FileResponse.From(f, isNew: false)).ToList();
    }
}
