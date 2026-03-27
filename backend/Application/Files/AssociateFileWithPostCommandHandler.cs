using Application.Abstractions;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Files;

public class AssociateFileWithPostCommandHandler(CmsDbContext db) : ICommandHandler<AssociateFileWithPostCommand, bool>
{
    public async Task<bool> Handle(AssociateFileWithPostCommand request, CancellationToken cancellationToken)
    {
        var file = await db.FileItems
            .Include(f => f.Posts)
            .FirstOrDefaultAsync(f => f.Id == request.FileId, cancellationToken);

        var post = await db.Posts.FirstOrDefaultAsync(p => p.Id == request.PostId, cancellationToken);

        if (file is null || post is null)
        {
            return false;
        }

        if (file.Posts.Any(p => p.Id == post.Id))
        {
            return true;
        }

        file.Posts.Add(post);
        await db.SaveChangesAsync(cancellationToken);

        return true;
    }
}
