using Application.Abstractions;
using Infrastructure.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Application.Files;

public class DeleteFileCommandHandler(
    CmsDbContext db,
    IWebHostEnvironment env
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

        var basePath = env.ContentRootPath;
        var filePath = Path.Combine(basePath, file.StoragePath);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        db.FileItems.Remove(file);
        await db.SaveChangesAsync(cancellationToken);

        return true;
    }
}
