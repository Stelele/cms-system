namespace Domain.Posts;

/// <summary>
/// The three project blogs. This is the single table of truth for which blog
/// holds which category, shared by the importer, the validators and the CMS
/// admin UI so they cannot disagree.
/// </summary>
public static class ProjectBlogs
{
    public sealed record Definition(
        ProjectCategory Category,
        string Slug,
        string Label,
        string Icon);

    public static readonly IReadOnlyList<Definition> All =
    [
        new(ProjectCategory.GameDev, "game-dev", "Game Dev Projects", "i-ph-game-controller"),
        new(ProjectCategory.Graphics, "graphics", "Graphics Projects", "i-ph-polygon"),
        new(ProjectCategory.BusinessCase, "business-case", "Business Case Projects", "i-heroicons-briefcase"),
    ];

    public static Definition For(ProjectCategory category) =>
        All.Single(d => d.Category == category);

    public static Definition? ForSlug(string slug) =>
        All.FirstOrDefault(d => d.Slug == slug);

    public static bool IsProjectSlug(string slug) => ForSlug(slug) is not null;

    /// <summary>Lowercase slug for a category, used as the denormalised Tag value.</summary>
    public static string SlugFor(ProjectCategory category) => For(category).Slug;
}
