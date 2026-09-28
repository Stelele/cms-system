using Application.Abstractions;
using Application.DTOs;
using Domain.Posts;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Projects;

public record GetProjectBySlugQuery(string Slug) : IQuery<ProjectResponse?>;

public class GetProjectBySlugQueryHandler(CmsDbContext db)
    : IQueryHandler<GetProjectBySlugQuery, ProjectResponse?>
{
    public async Task<ProjectResponse?> Handle(
        GetProjectBySlugQuery request,
        CancellationToken cancellationToken)
    {
        var project = await db.Posts
            .OfType<Project>()
            .Where(p => p.Slug == request.Slug && p.IsPublished)
            .FirstOrDefaultAsync(cancellationToken);

        return project is null ? null : ProjectResponse.FromDomain(project);
    }
}
