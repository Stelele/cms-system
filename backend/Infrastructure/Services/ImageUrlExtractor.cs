using System.Text.RegularExpressions;

namespace Infrastructure.Services;

public static partial class ImageUrlExtractor
{
    [GeneratedRegex(@"!\[.*?\]\(([^\s""']+)(?:\s+""[^""]*"")?\)")]
    private static partial Regex MarkdownImageRegex();

    [GeneratedRegex(@"<img[^>]*\bsrc\s*=\s*[""']([^""']+)[""'][^>]*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlImageRegex();

    public static List<string> ExtractImageUrls(string? content, string? coverImageUrl, string publicBucketUrlPrefix)
    {
        var urls = new List<string>();

        if (!string.IsNullOrEmpty(coverImageUrl) && coverImageUrl.StartsWith(publicBucketUrlPrefix))
            urls.Add(coverImageUrl);

        if (!string.IsNullOrEmpty(content))
        {
            foreach (Match match in MarkdownImageRegex().Matches(content))
                if (match.Groups[1].Value.StartsWith(publicBucketUrlPrefix))
                    urls.Add(match.Groups[1].Value);

            foreach (Match match in HtmlImageRegex().Matches(content))
                if (match.Groups[1].Value.StartsWith(publicBucketUrlPrefix))
                    urls.Add(match.Groups[1].Value);
        }

        return urls;
    }
}
