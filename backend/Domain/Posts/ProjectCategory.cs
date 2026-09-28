namespace Domain.Posts;

/// <summary>
/// The closed set of project categories. Each value names the blog that holds
/// its projects, so category is expressed by placement rather than a free-form
/// string. The mapping lives in <see cref="ProjectBlogs"/>.
/// </summary>
public enum ProjectCategory
{
    GameDev = 0,
    Graphics = 1,
    BusinessCase = 2,
}
