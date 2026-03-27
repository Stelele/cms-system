using Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Application.Files;

public record UploadFileCommand(
    IFormFile File,
    string? AltText = null
) : ICommand<FileResponse>;
