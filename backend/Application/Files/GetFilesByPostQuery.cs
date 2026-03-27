using Application.Abstractions;

namespace Application.Files;

public record GetFilesByPostQuery(Guid PostId) : IQuery<List<FileResponse>>;
