using Application.Abstractions;
using Domain.Blogs;
using Domain.Posts;
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

        var blog = Blog.Create(request.Name, request.Slug, request.Description, request.Icon);
        blog.Kind = request.Kind;

        // A Project blog is only meaningful with one of the three registered
        // project slugs, so refuse anything else rather than creating a blog
        // that no project can ever be written into.
        if (blog.Kind == BlogKind.Project && !ProjectBlogs.IsProjectSlug(blog.Slug))
            throw new InvalidOperationException(
                $"A project blog must use one of: {string.Join(", ", ProjectBlogs.All.Select(d => d.Slug))}.");

        await db.Blogs.AddAsync(blog, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return blog.Id;
    }
}
