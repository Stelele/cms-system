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

        var slugExists = await db.Blogs
            .AnyAsync(b => b.Slug == request.Slug && b.Id != request.Id, cancellationToken);

        if (slugExists)
            throw new InvalidOperationException($"A blog with slug '{request.Slug}' already exists.");

        blog.Name = request.Name;
        blog.Slug = request.Slug;
        blog.Description = request.Description;
        blog.UpdatedOn = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return true;
    }
}
