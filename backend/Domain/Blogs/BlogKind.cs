namespace Domain.Blogs;

/// <summary>
/// Decides which endpoint family may write into a blog, and therefore what
/// content type the blog holds. A blog is one or the other, never both.
/// </summary>
public enum BlogKind
{
    Standard = 0,
    Project = 1,
}
