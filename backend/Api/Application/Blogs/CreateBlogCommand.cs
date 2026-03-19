using Application.Abstractions;
using FluentValidation;

namespace Application.Blogs;

public record CreateBlogCommand(
    string Name,
    string Slug,
    string Description
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
    }
}
