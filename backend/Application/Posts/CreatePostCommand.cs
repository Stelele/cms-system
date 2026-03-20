using Application.Abstractions;
using FluentValidation;

namespace Application.Posts;

public record CreatePostCommand(
    Guid BlogId,
    string Title,
    string Slug,
    string Content,
    string Tag,
    string? CoverImageUrl,
    bool IsPublished
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
    }
}
