using Application.Abstractions;
using Application.DTOs;

namespace Application.Posts;

public record GetPostBySlugQuery(string Slug) : IQuery<PostResponse?>;
