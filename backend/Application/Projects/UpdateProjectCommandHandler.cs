using Application.Abstractions;
using Domain.Blogs;
using Domain.Posts;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Application.Projects;

public class UpdateProjectCommandHandler(CmsDbContext db, FileReferenceService fileRefService)
    : ICommandHandler<UpdateProjectCommand, bool>
{
    public async Task<bool> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        // Looked up on Post rather than OfType<Project> so that the boundary
        // message is what a caller editing an ordinary post through this
        // endpoint sees, instead of a silent 404. Not-found still returns false
        // before any guard, exactly as the post update does.
        var post = await db.Posts
            .FirstOrDefaultAsync(p => p.BlogId == request.BlogId && p.Id == request.Id, cancellationToken);

        if (post is null) return false;

        var blog = await db.Blogs.FirstOrDefaultAsync(b => b.Id == request.BlogId, cancellationToken);
        if (blog is null)
            throw new KeyNotFoundException($"Blog with ID '{request.BlogId}' not found.");

        if (blog.Kind != BlogKind.Project)
            throw new InvalidOperationException(
                $"Blog '{blog.Slug}' holds ordinary posts. Use the post endpoints.");

        if (ProjectBlogs.SlugFor(request.Category) != blog.Slug)
            throw new InvalidOperationException(
                $"Category '{request.Category}' belongs to blog '{ProjectBlogs.SlugFor(request.Category)}', "
                + $"not '{blog.Slug}'.");

        // A plain Post inside a project blog cannot exist through either
        // endpoint family; treat it as absent rather than casting it away.
        if (post is not Project project) return false;

        var slugExists = await db.Posts
            .AnyAsync(p => p.BlogId == request.BlogId && p.Slug == request.Slug && p.Id != request.Id, cancellationToken);
        if (slugExists)
            throw new InvalidOperationException($"A post with slug '{request.Slug}' already exists in this blog.");

        project.Title = request.Title;
        project.Slug = request.Slug;
        project.Content = request.Content;
        project.Description = request.Description;
        project.Category = request.Category;
        // Tag is denormalised from the category on create; keep it in step so a
        // read that falls back to Tag cannot contradict Category.
        project.Tag = ProjectBlogs.SlugFor(request.Category);
        project.Year = request.Year;
        project.Stack = request.Stack ?? [];
        project.LastPushedAt = request.LastPushedAt;
        project.Links = (request.Links ?? [])
            .Select(l => new ProjectLink { Label = l.Label, Url = l.Url })
            .ToList();
        project.CoverImageUrl = request.CoverImageUrl;
        project.UpdatedOn = DateTimeOffset.UtcNow;

        if (request.IsPublished && !project.IsPublished)
        {
            project.Publish();

            if (request.PublishedOn.HasValue)
                project.SetPublishedOn(request.PublishedOn.Value);
        }
        else if (!request.IsPublished && project.IsPublished)
        {
            project.Unpublish();
        }
        else if (request.IsPublished && request.PublishedOn.HasValue)
        {
            // Already published, but the date is being corrected.
            project.SetPublishedOn(request.PublishedOn.Value);
        }

        await db.SaveChangesAsync(cancellationToken);

        await fileRefService.ReconcilePostFilesAsync(
            project.Id, project.Content, project.CoverImageUrl, cancellationToken);

        return true;
    }
}
