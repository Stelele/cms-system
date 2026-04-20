using Application.Abstractions;
using FluentValidation;

namespace Application.Blogs;

public record CreateBlogCommand(
    string Name,
    string Slug,
    string Description,
    string Icon
) : ICommand<Guid>;

public sealed class CreateBlogCommandValidator : AbstractValidator<CreateBlogCommand>
{
    public CreateBlogCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty();

        RuleFor(x => x.Slug)
            .NotEmpty()
            .Matches("^[a-z0-9-]+$")
            .WithMessage("Slug must contain only lowercase letters, numbers, and hyphens.");

        RuleFor(x => x.Description)
            .NotEmpty();

        RuleFor(x => x.Icon)
            .NotEmpty()
            .Matches(@"^i-[a-z0-9-]+$")
            .WithMessage("Icon must be a valid Iconify class (e.g., i-heroicons-book-open)");
    }
}
