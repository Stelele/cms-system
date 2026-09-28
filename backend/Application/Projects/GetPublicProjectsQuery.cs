using Application.Abstractions;
using Application.DTOs;
using Domain.Posts;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Projects;

/// <summary>
/// The anonymous project read. Published-only is a constant, not a parameter,
/// so a caller cannot opt out of it - and it lives here rather than in the
/// authenticated query so that giving the admin UI access to drafts cannot
/// weaken what the public route is allowed to return.
/// </summary>
public record GetPublicProjectsQuery(ProjectCategory? Category = null) : IQuery<List<ProjectSummaryResponse>>;

public class GetPublicProjectsQueryHandler(CmsDbContext db)
    : IQueryHandler<GetPublicProjectsQuery, List<ProjectSummaryResponse>>
{
    public async Task<List<ProjectSummaryResponse>> Handle(
        GetPublicProjectsQuery request,
        CancellationToken cancellationToken)
    {
        // OfType<Project> is what excludes ordinary posts.
        var query = db.Posts.OfType<Project>().Where(p => p.IsPublished);

        if (request.Category is { } category)
            query = query.Where(p => p.Category == category);

        var projects = await query
            .OrderByDescending(p => p.Year)
            .ThenBy(p => p.Title)
            .ToListAsync(cancellationToken);

        return projects.Select(ProjectSummaryResponse.FromDomain).ToList();
    }
}

public record GetPublicProjectBySlugQuery(string Slug) : IQuery<ProjectResponse?>;

public class GetPublicProjectBySlugQueryHandler(CmsDbContext db)
    : IQueryHandler<GetPublicProjectBySlugQuery, ProjectResponse?>
{
    public async Task<ProjectResponse?> Handle(
        GetPublicProjectBySlugQuery request,
        CancellationToken cancellationToken)
    {
        var project = await db.Posts
            .OfType<Project>()
            .Where(p => p.Slug == request.Slug && p.IsPublished)
            .FirstOrDefaultAsync(cancellationToken);

        return project is null ? null : ProjectResponse.FromDomain(project);
    }
}
