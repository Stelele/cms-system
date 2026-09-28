using Domain.Posts;

namespace Application.Projects;

public enum ProjectStatus
{
    Active = 0,
    Archived = 1,
}

public static class ProjectStatusRules
{
    public const int ActiveWindowDays = 365;

    /// <summary>
    /// Derived, never stored: changing the window reclassifies every project
    /// with no data migration. A null last-push is archived rather than active,
    /// because an unknown state must not read as "still being worked on".
    /// </summary>
    public static ProjectStatus Derive(DateTimeOffset? lastPushedAt) =>
        lastPushedAt is { } pushed &&
        pushed >= DateTimeOffset.UtcNow.AddDays(-ActiveWindowDays)
            ? ProjectStatus.Active
            : ProjectStatus.Archived;
}
