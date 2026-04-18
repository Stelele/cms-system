using Application.Abstractions;
using Application.DTOs;

namespace Application.Blogs;

public record GetBlogsQuery(string[]? Slugs = null) : IQuery<List<BlogResponse>>;
