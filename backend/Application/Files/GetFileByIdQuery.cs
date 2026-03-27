using Application.Abstractions;

namespace Application.Files;

public record GetFileByIdQuery(Guid Id) : IQuery<FileResponse?>;
