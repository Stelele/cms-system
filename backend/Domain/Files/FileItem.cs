using Domain.Posts;

namespace Domain.Files;

public class FileItem : Abstractions.Base
{
    public string FileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string? AltText { get; set; }

    public ICollection<Post> Posts { get; set; } = [];
}
