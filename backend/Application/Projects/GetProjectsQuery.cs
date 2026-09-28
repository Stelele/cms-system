using Application.Abstractions;
using Application.DTOs;
using Domain.Posts;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Projects;

public record GetProjectsQuery(ProjectCategory? Category = null) : IQuery<List<ProjectSummaryResponse>>;

public class GetProjectsQueryHandler(CmsDbContext db)
    : IQueryHandler<GetProjectsQuery, List<ProjectSummaryResponse>>
{
    public async Task<List<ProjectSummaryResponse>> Handle(
        GetProjectsQuery request,
        CancellationToken cancellationToken)
    {
        // No IsPublished filter: this is the authenticated read, and the admin
        // UI has to be able to list and edit drafts. The published-only guarantee
        // belongs to GetPublicProjectsQuery, which is a separate query precisely
        // so that loosening this one cannot weaken the public route.
        var query = db.Posts.OfType<Project>();

        if (request.Category is { } category)
            query = query.Where(p => p.Category == category);

        var projects = await query
            .OrderByDescending(p => p.Year)
            .ThenBy(p => p.Title)
            .ToListAsync(cancellationToken);

        return projects.Select(ProjectSummaryResponse.FromDomain).ToList();
    }
}
