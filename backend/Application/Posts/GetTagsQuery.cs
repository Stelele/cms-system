using Application.Abstractions;
using Application.DTOs;

namespace Application.Posts;

public record GetTagsQuery : IQuery<List<TagResponse>>;
