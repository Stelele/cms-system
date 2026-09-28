namespace Domain.Blogs;

/// <summary>
/// The content kinds the renderer knows how to display. Kept as string constants
/// rather than an enum because the wire contract is already these literals — the
/// frontend's Blog type is `'html' | 'markdown'` — and a serialisation change
/// would be a needless risk for a two-value set. The closed set is enforced at the
/// write boundary by <see cref="Blog.Create"/>, which throws when
/// <see cref="IsValid"/> fails, and at the read boundary by <see cref="OrDefault"/>,
/// which coerces anything else to <see cref="Default"/>.
/// </summary>
public static class BlogContentType
{
    public const string Markdown = "markdown";
    public const string Html = "html";

    public const string Default = Markdown;

    public static bool IsValid(string? value) =>
        value is null || value is Markdown or Html;

    /// <summary>
    /// Maps any input to one of the two legal values, coercing null and anything
    /// unrecognised to <see cref="Default"/>. A public read path calls this, so no
    /// third value can reach the wire even if a row somehow holds one.
    /// </summary>
    public static string OrDefault(string? value) =>
        value is Markdown or Html ? value : Default;
}
