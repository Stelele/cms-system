using Application.Abstractions;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Posts;

public class UpdatePostCommandHandler(CmsDbContext db) : ICommandHandler<UpdatePostCommand, bool>
{
    public async Task<bool> Handle(UpdatePostCommand request, CancellationToken cancellationToken)
    {
        var post = await db.Posts
            .FirstOrDefaultAsync(p => p.BlogId == request.BlogId && p.Id == request.Id, cancellationToken);

        if (post == null) return false;

        var slugExists = await db.Posts
            .AnyAsync(p => p.BlogId == request.BlogId && p.Slug == request.Slug && p.Id != request.Id, cancellationToken);

        if (slugExists)
            throw new InvalidOperationException($"A post with slug '{request.Slug}' already exists in this blog.");

        post.Title = request.Title;
        post.Slug = request.Slug;
        post.Content = request.Content;
        post.Description = request.Description;
        post.Tag = request.Tag;
        post.CoverImageUrl = request.CoverImageUrl;
        post.UpdatedOn = DateTimeOffset.UtcNow;

        if (request.IsPublished && !post.IsPublished)
            post.Publish();
        else if (!request.IsPublished && post.IsPublished)
            post.Unpublish();

        await db.SaveChangesAsync(cancellationToken);

        return true;
    }
}
