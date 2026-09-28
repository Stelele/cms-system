using Application.Abstractions;

namespace Application.Projects;

public record DeleteProjectCommand(Guid BlogId, Guid Id) : ICommand<bool>;
