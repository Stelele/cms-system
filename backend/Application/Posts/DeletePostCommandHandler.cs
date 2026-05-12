using Application.Abstractions;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Application.Posts;

public class DeletePostCommandHandler(CmsDbContext db, FileReferenceService fileRefService) : ICommandHandler<DeletePostCommand, bool>
{
    public async Task<bool> Handle(DeletePostCommand request, CancellationToken cancellationToken)
    {
        var post = await db.Posts
            .FirstOrDefaultAsync(p => p.BlogId == request.BlogId && p.Id == request.Id, cancellationToken);

        if (post == null) return false;

        db.Posts.Remove(post);
        await db.SaveChangesAsync(cancellationToken);

        await fileRefService.MarkOrphanedFilesAsync(cancellationToken);

        return true;
    }
}
