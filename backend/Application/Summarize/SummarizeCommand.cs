using Application.Abstractions;
using FluentValidation;
using System.Text.Json.Serialization;

namespace Application.Summarize;

public record SummarizeCommand : ICommand<string>
{
    [JsonPropertyName("content")]
    public required string Content { get; init; }
}

public sealed class SummarizeCommandValidator : AbstractValidator<SummarizeCommand>
{
    public SummarizeCommandValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Content is required for summarization.");
    }
}
