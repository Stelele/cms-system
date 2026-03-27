using Application.Abstractions;

namespace Application.Files;

public record DeleteFileCommand(Guid Id) : ICommand<bool>;
