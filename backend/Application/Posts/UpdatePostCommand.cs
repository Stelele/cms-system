using Application.Abstractions;
using FluentValidation;
using System.Text.Json.Serialization;

namespace Application.Posts;

public record UpdatePostCommand(
    [property: JsonPropertyName("blogId")] Guid BlogId,
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("tag")] string Tag,
    [property: JsonPropertyName("coverImageUrl")] string? CoverImageUrl,
    [property: JsonPropertyName("isPublished")] bool IsPublished,
    [property: JsonPropertyName("publishedOn")] DateTimeOffset? PublishedOn = null,
    [property: JsonPropertyName("canonicalUrl")] string? CanonicalUrl = null
) : ICommand<bool>;

public sealed class UpdatePostCommandValidator : AbstractValidator<UpdatePostCommand>
{
    public UpdatePostCommandValidator()
    {
        RuleFor(x => x.BlogId)
            .NotEmpty();

        RuleFor(x => x.Id)
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

        RuleFor(x => x.CanonicalUrl)
            .Must(BeAbsoluteHttpUrl)
            .When(x => !string.IsNullOrEmpty(x.CanonicalUrl))
            .WithMessage("CanonicalUrl must be an absolute http or https URL.");
    }

    private static bool BeAbsoluteHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
