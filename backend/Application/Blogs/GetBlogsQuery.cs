using Application.Abstractions;
using Application.DTOs;

namespace Application.Blogs;

public record GetBlogsQuery : IQuery<List<BlogResponse>>;
