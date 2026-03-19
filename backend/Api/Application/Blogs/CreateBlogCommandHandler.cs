using Application.Abstractions;
using Domain.Blogs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Blogs;

public class CreateBlogCommandHandler(CmsDbContext db) : ICommandHandler<CreateBlogCommand, Guid>
{
    public async Task<Guid> Handle(CreateBlogCommand request, CancellationToken cancellationToken)
    {
        var slugExists = await db.Blogs
            .AnyAsync(b => b.Slug == request.Slug, cancellationToken);

        if (slugExists)
            throw new InvalidOperationException($"A blog with slug '{request.Slug}' already exists.");

        var blog = Blog.Create(request.Name, request.Slug, request.Description);

        await db.Blogs.AddAsync(blog, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return blog.Id;
    }
}
