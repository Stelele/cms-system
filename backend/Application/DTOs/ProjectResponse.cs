using Application.Projects;
using Domain.Posts;

namespace Application.DTOs;

/// <summary>
/// The full article projection. Deliberately omits IsPublished, CreatedOn and
/// UpdatedOn so internal bookkeeping cannot reach a public caller.
/// </summary>
public record ProjectResponse(
    Guid Id,
    Guid BlogId,
    string Slug,
    string Title,
    string Content,
    string? Description,
    ProjectCategory Category,
    List<string> Stack,
    int Year,
    DateTimeOffset? LastPushedAt,
    List<ProjectLink> Links,
    string? CoverImageUrl,
    DateTimeOffset? PublishedOn,
    string? CanonicalUrl,
    ProjectStatus Status)
{
    public static ProjectResponse FromDomain(Project project) =>
        new(project.Id, project.BlogId, project.Slug, project.Title, project.Content,
            project.Description, project.Category, project.Stack, project.Year,
            project.LastPushedAt, project.Links, project.CoverImageUrl, project.PublishedOn,
            project.CanonicalUrl, ProjectStatusRules.Derive(project.LastPushedAt));
}
