using Application.Abstractions;
using Domain.Blogs;
using Domain.Posts;
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

        if (request.Kind == BlogKind.Project && !ProjectBlogs.IsProjectSlug(blog.Slug))
            throw new InvalidOperationException(
                $"A project blog must use one of: {string.Join(", ", ProjectBlogs.All.Select(d => d.Slug))}.");

        blog.Kind = request.Kind;
        blog.UpdatedOn = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return true;
    }
}
