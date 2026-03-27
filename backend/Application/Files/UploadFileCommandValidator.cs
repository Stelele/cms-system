using FluentValidation;

namespace Application.Files;

public sealed class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
    ];

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB

    public UploadFileCommandValidator()
    {
        RuleFor(x => x.File)
            .NotNull()
            .WithMessage("File is required.");

        RuleFor(x => x.File.ContentType)
            .Must(AllowedContentTypes.Contains)
            .WithMessage($"File type must be one of: {string.Join(", ", AllowedContentTypes)}.");

        RuleFor(x => x.File.Length)
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage($"File size must not exceed {MaxFileSizeBytes / (1024 * 1024)}MB.");
    }
}
