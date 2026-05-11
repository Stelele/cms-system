using Application.Abstractions;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Application.Files;

public class DeleteFileCommandHandler(
    CmsDbContext db,
    IR2StorageService r2
) : ICommandHandler<DeleteFileCommand, bool>
{
    public async Task<bool> Handle(DeleteFileCommand request, CancellationToken cancellationToken)
    {
        var file = await db.FileItems
            .Include(f => f.Posts)
            .FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken);

        if (file is null)
        {
            return false;
        }

        if (file.Posts.Count > 0)
        {
            return false;
        }

        await r2.DeleteAsync(r2.PublicBucket, file.StoragePath, cancellationToken);

        db.FileItems.Remove(file);
        await db.SaveChangesAsync(cancellationToken);

        return true;
    }
}
