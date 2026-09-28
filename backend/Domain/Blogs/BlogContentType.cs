namespace Domain.Blogs;

/// <summary>
/// The content kinds the renderer knows how to display. Kept as string constants
/// rather than an enum because the wire contract is already these literals — the
/// frontend's Blog type is `'html' | 'markdown'` — and a serialisation change
/// would be a needless risk for a two-value set. The closed set is enforced by
/// <see cref="IsValid"/> and by the command validators.
/// </summary>
public static class BlogContentType
{
    public const string Markdown = "markdown";
    public const string Html = "html";

    public const string Default = Markdown;

    public static bool IsValid(string? value) =>
        value is null || value is Markdown or Html;

    /// <summary>Normalises null to the default so callers never branch on it.</summary>
    public static string OrDefault(string? value) =>
        IsValid(value) ? value ?? Default : Default;
}
