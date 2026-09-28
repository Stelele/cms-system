using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Posts;

namespace Seeder;

public sealed record ManifestEntry(
    string Slug,
    [property: JsonPropertyName("repoName")] string RepoName,
    string Title,
    ProjectCategory Category,
    int Year,
    List<string> Stack,
    DateTimeOffset? LastPushedAt,
    List<ManifestLink> Links,
    string GitHubDescription);

public sealed record ManifestLink(string Label, string Url);

public static class Manifest
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static List<ManifestEntry> Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Manifest not found.", path);

        var json = File.ReadAllText(path);
        var entries = JsonSerializer.Deserialize<List<ManifestEntry>>(json, Options)
            ?? throw new InvalidOperationException($"Manifest {path} deserialised to null.");

        if (entries.Count == 0)
            throw new InvalidOperationException($"Manifest {path} is empty.");

        var duplicates = entries.GroupBy(e => e.Slug, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicates.Count > 0)
            throw new InvalidOperationException(
                $"Manifest has duplicate slugs: {string.Join(", ", duplicates)}");

        foreach (var e in entries)
        {
            if (!ProjectBlogs.All.Any(d => d.Category == e.Category))
                throw new InvalidOperationException(
                    $"Manifest entry '{e.Slug}' has category '{e.Category}', which is not a project category.");
            if (e.Year is < 2000 or > 2100)
                throw new InvalidOperationException(
                    $"Manifest entry '{e.Slug}' has year {e.Year}, which the validator would reject.");
        }

        return entries;
    }
}

public sealed record Options(
    string ConnectionString,
    string ManifestPath,
    string PublicBucketUrl,
    string? MediatrLicenseKey,
    bool DryRun,
    bool DeleteLegacyProjectsBlog)
{
    public static Options? Parse(string[] args)
    {
        string? connection = null, manifest = null, bucketUrl = null;
        string? license = null;
        var dryRun = false;
        var deleteLegacy = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--connection" when i + 1 < args.Length: connection = args[++i]; break;
                case "--manifest" when i + 1 < args.Length: manifest = args[++i]; break;
                case "--public-bucket-url" when i + 1 < args.Length: bucketUrl = args[++i]; break;
                case "--mediatr-license" when i + 1 < args.Length: license = args[++i]; break;                case "--dry-run": dryRun = true; break;
                case "--delete-legacy-projects-blog": deleteLegacy = true; break;
                case "--help" or "-h": PrintHelp(); return null;
                default:
                    Console.Error.WriteLine($"Unrecognised argument '{args[i]}'.");
                    PrintHelp();
                    return null;
            }
        }

        if (string.IsNullOrWhiteSpace(connection) || string.IsNullOrWhiteSpace(manifest))
        {
            Console.Error.WriteLine("--connection and --manifest are both required.");
            PrintHelp();
            return null;
        }

        // MediatR is a licensed product here, so resolving IMediator needs the
        // key the CMS already runs with. Taking it from the environment keeps it
        // out of shell history and process listings.
        license ??= Environment.GetEnvironmentVariable("MEDIATR_LICENSE_KEY");
        if (string.IsNullOrWhiteSpace(license))
        {
            Console.Error.WriteLine(
                "No MediatR license key. Set MEDIATR_LICENSE_KEY or pass --mediatr-license; "
                + "the CMS container already has it as MediatR__LicenseKey.");
            return null;
        }

        return new Options(connection, manifest, bucketUrl ?? "https://media.invalid", license, dryRun, deleteLegacy);
    }

    private static void PrintHelp() => Console.WriteLine(
        """
        Seeder - creates the three project blogs and one draft project per manifest entry.

          --connection <sqlite path or connection string>   required
          --manifest <path to manifest.json>                required
          --public-bucket-url <url>                         used only to resolve media paths
          --mediatr-license <key>                           if the MediatR build needs it
          --dry-run                                         validate and print, write nothing
          --delete-legacy-projects-blog                     delete the empty legacy 'projects' blog

        Every project is created unpublished, so nothing seeded here is publicly visible.
        """);
}
