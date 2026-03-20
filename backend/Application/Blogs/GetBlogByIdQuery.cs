using Application.Abstractions;
using Application.DTOs;

namespace Application.Blogs;

public record GetBlogByIdQuery(Guid Id) : IQuery<BlogResponse?>;
