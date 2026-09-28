using Application.Abstractions;
using Domain.Posts;
using FluentValidation;
using System.Text.Json.Serialization;

namespace Application.Projects;

/// <summary>
/// Same shape as <see cref="CreateProjectCommand"/> plus the identity of the
/// row being edited. <c>Id</c> also arrives from the route, which overwrites it
/// with <c>with { Id = id }</c> the way the post and blog updates do.
/// </summary>
public record UpdateProjectCommand(
    [property: JsonPropertyName("blogId"), JsonRequired] Guid BlogId,
    [property: JsonPropertyName("id"), JsonRequired] Guid Id,
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
) : ICommand<bool>;

public sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.BlogId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
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
