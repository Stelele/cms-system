using Application.Abstractions;
using FluentValidation;

namespace Application.Posts;

public record UpdatePostCommand(
    Guid BlogId,
    Guid Id,
    string Title,
    string Slug,
    string Content,
    string? Description,
    string Tag,
    string? CoverImageUrl,
    bool IsPublished,
    DateTimeOffset? PublishedOn = null,
    string? CanonicalUrl = null
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
