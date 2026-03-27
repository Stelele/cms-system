using Domain.Files;

namespace Application.Files;

public record FileResponse(
    Guid Id,
    string FileName,
    string Url,
    string ContentType,
    long Size,
    string? AltText,
    bool IsNew
)
{
    public static FileResponse From(FileItem item, bool isNew) =>
        new(item.Id, item.FileName, item.Url, item.ContentType, item.Size, item.AltText, isNew);
}
