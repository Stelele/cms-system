using Application.Abstractions;
using FluentValidation;
using System.Text.Json.Serialization;

namespace Application.Posts;

public record CreatePostCommand(
    [property: JsonPropertyName("blogId"), JsonRequired] Guid BlogId,
    [property: JsonPropertyName("title"), JsonRequired] string Title,
    [property: JsonPropertyName("slug"), JsonRequired] string Slug,
    [property: JsonPropertyName("content"), JsonRequired] string Content,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("tag"), JsonRequired] string Tag,
    [property: JsonPropertyName("coverImageUrl"), JsonRequired] string? CoverImageUrl,
    [property: JsonPropertyName("isPublished"), JsonRequired] bool IsPublished,
    [property: JsonPropertyName("publishedOn")] DateTimeOffset? PublishedOn = null,
    [property: JsonPropertyName("canonicalUrl")] string? CanonicalUrl = null
) : ICommand<Guid>;

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

        RuleFor(x => x.CanonicalUrl)
            .Must(BeAbsoluteHttpUrl)
            .When(x => !string.IsNullOrEmpty(x.CanonicalUrl))
            .WithMessage("CanonicalUrl must be an absolute http or https URL.");
    }

    private static bool BeAbsoluteHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
