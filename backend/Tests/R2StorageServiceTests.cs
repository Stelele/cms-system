using Infrastructure.Services;

namespace Tests;

public class R2StorageServiceTests
{
    [Theory]
    [InlineData("image/jpeg", "images")]
    [InlineData("image/png", "images")]
    [InlineData("image/webp", "images")]
    [InlineData("image/svg+xml", "images")]
    [InlineData("image/bmp", "images")]
    [InlineData("image/tiff", "images")]
    [InlineData("image/gif", "gifs")]
    [InlineData("video/mp4", "videos")]
    [InlineData("video/webm", "videos")]
    [InlineData("video/quicktime", "videos")]
    [InlineData("application/pdf", "other")]
    [InlineData("text/plain", "other")]
    [InlineData("application/zip", "other")]
    [InlineData("", "other")]
    [InlineData(null, "other")]
    public void GetTypeFolder_ReturnsCorrectFolder(string? contentType, string expected)
    {
        var result = R2StorageService.GetTypeFolder(contentType!);
        Assert.Equal(expected, result);
    }
}
