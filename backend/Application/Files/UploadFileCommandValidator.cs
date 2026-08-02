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
        "image/svg+xml",
        "image/bmp",
        "image/tiff",
        "image/x-icon",
        "image/apng",
        "image/avif",
        "image/x-xbitmap",
        "audio/mpeg",
        "audio/wav",
        "audio/ogg",
        "audio/mp4",
        "audio/webm",
        "audio/aac",
        "audio/flac",
        "audio/x-m4a",
        "audio/m4a",
    ];

    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50MB

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
