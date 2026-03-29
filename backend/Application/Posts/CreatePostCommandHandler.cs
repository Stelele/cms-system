using Application.Abstractions;
using Domain.Posts;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Posts;

public class CreatePostCommandHandler(CmsDbContext db) : ICommandHandler<CreatePostCommand, Guid>
{
    public async Task<Guid> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        var blogExists = await db.Blogs
            .AnyAsync(b => b.Id == request.BlogId, cancellationToken);

        if (!blogExists)
            throw new KeyNotFoundException($"Blog with ID '{request.BlogId}' not found.");

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
            request.CoverImageUrl);

        if (request.IsPublished)
            post.Publish();

        await db.Posts.AddAsync(post, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return post.Id;
    }
}
