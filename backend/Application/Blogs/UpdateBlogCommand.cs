using Application.Abstractions;
using FluentValidation;

namespace Application.Blogs;

public record UpdateBlogCommand(
    Guid Id,
    string Name,
    string Description,
    string Icon
) : ICommand<bool>;

public sealed class UpdateBlogCommandValidator : AbstractValidator<UpdateBlogCommand>
{
    public UpdateBlogCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty();

        RuleFor(x => x.Description)
            .NotEmpty();

        RuleFor(x => x.Icon)
            .NotEmpty()
            .Matches(@"^i-[a-z0-9-]+$")
            .WithMessage("Icon must be a valid Iconify class (e.g., i-heroicons-book-open)");
    }
}