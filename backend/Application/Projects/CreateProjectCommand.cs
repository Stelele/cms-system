using Application.Abstractions;
using Domain.Posts;
using FluentValidation;
using System.Text.Json.Serialization;

namespace Application.Projects;

public sealed record ProjectLinkInput(
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("url")] string Url);

/// <summary>
/// The project write contract. The four typed fields (category, year, stack,
/// links) are deliberately NOT <c>[JsonRequired]</c>: the archive importer posts
/// a project in stages, so a payload without them is legal and the validator
/// and handler decide what it means. Everything a post already requires is
/// required here too - losing <c>[JsonRequired]</c> on <c>IsPublished</c> is the
/// exact defect that silently turned a payload into a draft (see the
/// <c>post commands'</c> required-field history).
/// </summary>
public record CreateProjectCommand(
    [property: JsonPropertyName("blogId"), JsonRequired] Guid BlogId,
    [property: JsonPropertyName("title"), JsonRequired] string Title,
    [property: JsonPropertyName("slug"), JsonRequired] string Slug,
    [property: JsonPropertyName("content"), JsonRequired] string Content,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("category")] ProjectCategory Category,
    [property: JsonPropertyName("year")] int Year,
    [property: JsonPropertyName("stack")] List<string> Stack,
    [property: JsonPropertyName("lastPushedAt")] DateTimeOffset? LastPushedAt,
    [property: JsonPropertyName("links")] List<ProjectLinkInput> Links,
    [property: JsonPropertyName("coverImageUrl"), JsonRequired] string? CoverImageUrl,
    [property: JsonPropertyName("isPublished"), JsonRequired] bool IsPublished,
    [property: JsonPropertyName("publishedOn")] DateTimeOffset? PublishedOn = null
) : ICommand<Guid>;

public sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.BlogId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.Slug).NotEmpty().Matches("^[a-z0-9-]+$")
            .WithMessage("Slug must contain only lowercase letters, numbers, and hyphens.");
        RuleFor(x => x.Content).NotEmpty();
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleForEach(x => x.Links).ChildRules(link =>
        {
            link.RuleFor(l => l.Label).NotEmpty();
            link.RuleFor(l => l.Url).Must(BeAbsoluteHttpUrl)
                .WithMessage("Link url must be an absolute http or https URL.");
        });
    }

    private static bool BeAbsoluteHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
