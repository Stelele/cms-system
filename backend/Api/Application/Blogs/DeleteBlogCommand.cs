using Application.Abstractions;

namespace Application.Blogs;

public record DeleteBlogCommand(Guid Id) : ICommand<bool>;
