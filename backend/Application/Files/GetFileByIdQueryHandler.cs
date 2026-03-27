using Application.Abstractions;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Files;

public class GetFileByIdQueryHandler(CmsDbContext db) : IQueryHandler<GetFileByIdQuery, FileResponse?>
{
    public async Task<FileResponse?> Handle(GetFileByIdQuery request, CancellationToken cancellationToken)
    {
        var file = await db.FileItems.FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken);
        return file is null ? null : FileResponse.From(file, isNew: false);
    }
}
