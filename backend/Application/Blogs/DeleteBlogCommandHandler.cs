using Application.Abstractions;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Blogs;

public class DeleteBlogCommandHandler(CmsDbContext db) : ICommandHandler<DeleteBlogCommand, bool>
{
    public async Task<bool> Handle(DeleteBlogCommand request, CancellationToken cancellationToken)
    {
        var blog = await db.Blogs
            .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);

        if (blog == null) return false;

        db.Blogs.Remove(blog);
        await db.SaveChangesAsync(cancellationToken);

        return true;
    }
}
