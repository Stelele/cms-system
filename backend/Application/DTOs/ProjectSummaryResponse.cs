using Application.Projects;
using Domain.Posts;

namespace Application.DTOs;

/// <summary>
/// The listing projection. Omits Content deliberately: 53 articles in one
/// payload is roughly 150KB for a grid of cards, and a field not on the record
/// cannot be serialised.
/// </summary>
public record ProjectSummaryResponse(
    Guid Id,
    Guid BlogId,
    string Slug,
    string Title,
    string? Description,
    ProjectCategory Category,
    List<string> Stack,
    int Year,
    DateTimeOffset? LastPushedAt,
    List<ProjectLink> Links,
    string? CoverImageUrl,
    DateTimeOffset? PublishedOn,
    ProjectStatus Status)
{
    public static ProjectSummaryResponse FromDomain(Project project) =>
        new(project.Id, project.BlogId, project.Slug, project.Title, project.Description,
            project.Category, project.Stack, project.Year, project.LastPushedAt,
            project.Links, project.CoverImageUrl, project.PublishedOn,
            ProjectStatusRules.Derive(project.LastPushedAt));
}
