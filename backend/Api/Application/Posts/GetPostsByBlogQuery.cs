using Application.Abstractions;
using Application.DTOs;

namespace Application.Posts;

public record GetPostsByBlogQuery(Guid BlogId, string? Tag = null, bool? IsPublished = null) : IQuery<List<PostResponse>>;
