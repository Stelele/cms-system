using Application.Abstractions;

namespace Application.Files;

public record AssociateFileWithPostCommand(Guid FileId, Guid PostId) : ICommand<bool>;
