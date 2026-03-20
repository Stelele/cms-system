using Application.Abstractions;
using FluentValidation;

namespace Application.Blogs;

public record UpdateBlogCommand(
    Guid Id,
    string Name,
    string Slug,
    string Description
) : ICommand<bool>;

public sealed class UpdateBlogCommandValidator : AbstractValidator<UpdateBlogCommand>
{
    public UpdateBlogCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty();

        RuleFor(x => x.Slug)
            .NotEmpty()
            .Matches("^[a-z0-9-]+$")
            .WithMessage("Slug must contain only lowercase letters, numbers, and hyphens.");

        RuleFor(x => x.Description)
            .NotEmpty();
    }
}
