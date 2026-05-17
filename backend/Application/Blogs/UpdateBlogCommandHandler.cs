using Application.Abstractions;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Blogs;

public class UpdateBlogCommandHandler(CmsDbContext db) : ICommandHandler<UpdateBlogCommand, bool>
{
    public async Task<bool> Handle(UpdateBlogCommand request, CancellationToken cancellationToken)
    {
        var blog = await db.Blogs
            .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);

        if (blog == null) return false;

        blog.Name = request.Name;
        blog.Description = request.Description;
        blog.Icon = request.Icon;
        blog.UpdatedOn = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return true;
    }
}
