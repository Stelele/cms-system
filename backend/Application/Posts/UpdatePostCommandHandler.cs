using Application.Abstractions;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Application.Posts;

public class UpdatePostCommandHandler(CmsDbContext db, FileReferenceService fileRefService) : ICommandHandler<UpdatePostCommand, bool>
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
        post.CanonicalUrl = request.CanonicalUrl;
        post.UpdatedOn = DateTimeOffset.UtcNow;

        if (request.IsPublished && !post.IsPublished)
        {
            post.Publish();

            if (request.PublishedOn.HasValue)
                post.SetPublishedOn(request.PublishedOn.Value);
        }
        else if (!request.IsPublished && post.IsPublished)
        {
            post.Unpublish();
        }
        else if (request.IsPublished && request.PublishedOn.HasValue)
        {
            // Already published, but the date is being corrected.
            post.SetPublishedOn(request.PublishedOn.Value);
        }

        await db.SaveChangesAsync(cancellationToken);

        await fileRefService.ReconcilePostFilesAsync(post.Id, post.Content, post.CoverImageUrl, cancellationToken);

        return true;
    }
}
