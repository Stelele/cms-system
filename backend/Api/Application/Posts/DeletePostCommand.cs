using Application.Abstractions;

namespace Application.Posts;

public record DeletePostCommand(Guid BlogId, Guid Id) : ICommand<bool>;
