using Application.Abstractions;
using Domain.Blogs;
using Domain.Posts;
using Infrastructure.Services;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Posts;

public class CreatePostCommandHandler(CmsDbContext db, FileReferenceService fileRefService) : ICommandHandler<CreatePostCommand, Guid>
{
    public async Task<Guid> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        var blog = await db.Blogs.FirstOrDefaultAsync(b => b.Id == request.BlogId, cancellationToken);
        if (blog is null)
            throw new KeyNotFoundException($"Blog with ID '{request.BlogId}' not found.");

        if (blog.Kind == BlogKind.Project)
            throw new InvalidOperationException(
                $"Blog '{blog.Slug}' holds projects. Use the project endpoints.");

        var slugExists = await db.Posts
            .AnyAsync(p => p.BlogId == request.BlogId && p.Slug == request.Slug, cancellationToken);

        if (slugExists)
            throw new InvalidOperationException($"A post with slug '{request.Slug}' already exists in this blog.");

        var post = Post.Create(
            request.BlogId,
            request.Title,
            request.Slug,
            request.Content,
            request.Description,
            request.Tag,
            request.CoverImageUrl,
            request.CanonicalUrl);

        if (request.IsPublished)
        {
            post.Publish();

            // Publish() stamps UtcNow; an explicit date is an archive import
            // correcting it, so it has to win.
            if (request.PublishedOn.HasValue)
                post.SetPublishedOn(request.PublishedOn.Value);
        }

        await db.Posts.AddAsync(post, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await fileRefService.ReconcilePostFilesAsync(post.Id, post.Content, post.CoverImageUrl, cancellationToken);

        return post.Id;
    }
}
