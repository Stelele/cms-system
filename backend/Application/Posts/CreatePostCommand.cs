using Application.Abstractions;
using FluentValidation;
using System.Text.Json.Serialization;

namespace Application.Posts;

public record CreatePostCommand : ICommand<Guid>
{
    [JsonPropertyName("blogId")]
    public required Guid BlogId { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("slug")]
    public required string Slug { get; init; }

    [JsonPropertyName("content")]
    public required string Content { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("tag")]
    public required string Tag { get; init; }

    [JsonPropertyName("coverImageUrl")]
    public required string? CoverImageUrl { get; init; }

    [JsonPropertyName("isPublished")]
    public required bool IsPublished { get; init; }
}

public sealed class CreatePostCommandValidator : AbstractValidator<CreatePostCommand>
{
    public CreatePostCommandValidator()
    {
        RuleFor(x => x.BlogId)
            .NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty();

        RuleFor(x => x.Slug)
            .NotEmpty()
            .Matches("^[a-z0-9-]+$")
            .WithMessage("Slug must contain only lowercase letters, numbers, and hyphens.");

        RuleFor(x => x.Content)
            .NotEmpty();

        RuleFor(x => x.Tag)
            .NotEmpty();
    }
}
