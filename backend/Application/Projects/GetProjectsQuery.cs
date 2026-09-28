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
        // IsPublished is a constant, not a parameter, so a caller cannot opt out
        // of it. OfType<Project> is what excludes ordinary posts.
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
