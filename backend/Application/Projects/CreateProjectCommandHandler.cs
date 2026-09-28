using Application.Abstractions;
using Domain.Blogs;
using Domain.Posts;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Application.Projects;

public class CreateProjectCommandHandler(CmsDbContext db, FileReferenceService fileRefService)
    : ICommandHandler<CreateProjectCommand, Guid>
{
    public async Task<Guid> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var blog = await db.Blogs.FirstOrDefaultAsync(b => b.Id == request.BlogId, cancellationToken);
        if (blog is null)
            throw new KeyNotFoundException($"Blog with ID '{request.BlogId}' not found.");

        // The write boundary. A standard blog cannot hold a project: without
        // typed fields the row would be an ordinary post wearing a category.
        if (blog.Kind != BlogKind.Project)
            throw new InvalidOperationException(
                $"Blog '{blog.Slug}' holds ordinary posts. Use the post endpoints.");

        // The category is denormalised onto the row, so it has to agree with the
        // blog that holds it or ProjectResponse would read back a lie.
        if (ProjectBlogs.SlugFor(request.Category) != blog.Slug)
            throw new InvalidOperationException(
                $"Category '{request.Category}' belongs to blog '{ProjectBlogs.SlugFor(request.Category)}', "
                + $"not '{blog.Slug}'.");

        var slugExists = await db.Posts
            .AnyAsync(p => p.BlogId == request.BlogId && p.Slug == request.Slug, cancellationToken);
        if (slugExists)
            throw new InvalidOperationException($"A post with slug '{request.Slug}' already exists in this blog.");

        var project = Project.Create(
            request.BlogId, request.Title, request.Slug, request.Content,
            request.Description, request.Category, request.Year);

        // Stack and Links are optional at the wire level (the importer posts in
        // stages), and a missing reference-type member arrives as null rather
        // than as an empty list.
        project.Stack = request.Stack ?? [];
        project.LastPushedAt = request.LastPushedAt;
        project.CoverImageUrl = request.CoverImageUrl;
        project.Links = (request.Links ?? [])
            .Select(l => new ProjectLink { Label = l.Label, Url = l.Url })
            .ToList();

        if (request.IsPublished)
        {
            project.Publish();

            // Publish() stamps UtcNow; an explicit date is an archive import
            // correcting it, so it has to win.
            if (request.PublishedOn.HasValue)
                project.SetPublishedOn(request.PublishedOn.Value);
        }

        await db.Posts.AddAsync(project, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await fileRefService.ReconcilePostFilesAsync(
            project.Id, project.Content, project.CoverImageUrl, cancellationToken);

        return project.Id;
    }
}
