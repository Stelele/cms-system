using Application.Abstractions;
using Application.DTOs;

namespace Application.Posts;

public record GetPostByIdQuery(Guid BlogId, Guid Id) : IQuery<PostResponse?>;
