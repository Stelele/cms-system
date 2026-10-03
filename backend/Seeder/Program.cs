using Application.Blogs;
using Application.PipelineBehaviours;
using Application.Posts;
using Application.Projects;
using Domain.Blogs;
using Domain.Posts;
using FluentValidation;
using Infrastructure.Models;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Seeder;

/// <summary>
/// Creates the three project category blogs and one unpublished draft project per
/// manifest entry, by dispatching the real application commands through MediatR.
///
/// It goes through the handlers rather than inserting rows directly so the seed is
/// subject to exactly the invariants an HTTP write faces: the project/post write
/// boundary, and a category that must agree with its blog. A hand-rolled insert
/// could drift from those rules and seed data the API itself would refuse.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var opts = Options.Parse(args);
        if (opts is null) return 2;

        var services = new ServiceCollection();
        // MediatR's LicenseAccessor takes a logger factory. The host gets one
        // from WebApplicationBuilder; a bare ServiceCollection does not, and
        // without it resolving IMediator throws.
        services.AddLogging();
        services.AddMediatR(cfg =>
        {
            cfg.LicenseKey = opts.MediatrLicenseKey;
            cfg.RegisterServicesFromAssembly(typeof(CreatePostCommand).Assembly);
        });
        services.AddValidatorsFromAssembly(typeof(CreatePostCommand).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        services.AddDbContext<CmsDbContext>(o => o.UseSqlite(opts.ConnectionString));
        services.AddSingleton<IR2StorageService>(_ => new OfflineR2(opts.PublicBucketUrl));
        services.AddScoped<FileReferenceService>();

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<CmsDbContext>();
        var mediatr = sp.GetRequiredService<IMediator>();

        var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
        if (pending.Count > 0)
        {
            Console.Error.WriteLine($"Refusing to seed: {pending.Count} migration(s) are not applied.");
            foreach (var p in pending) Console.Error.WriteLine($"  {p}");
            return 1;
        }

        var manifest = Manifest.Load(opts.ManifestPath);
        Console.WriteLine($"manifest: {manifest.Count} projects from {opts.ManifestPath}");

        // Articles live as markdown beside the manifest so the prose is
        // reviewable in a diff and editable without touching code. A missing
        // article falls back to the skeleton rather than silently seeding an
        // empty body.
        var articlesDir = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(opts.ManifestPath))!, "..", "articles");
        articlesDir = Path.GetFullPath(articlesDir);
        var bodies = new Dictionary<string, string>();
        if (Directory.Exists(articlesDir))
        {
            foreach (var f in Directory.EnumerateFiles(articlesDir, "*.md"))
                bodies[Path.GetFileNameWithoutExtension(f)] = File.ReadAllText(f);
        }
        var missing = manifest.Where(e => !bodies.ContainsKey(e.Slug)).Select(e => e.Slug).ToList();
        Console.WriteLine($"articles: {bodies.Count} found in {articlesDir}");
        if (missing.Count > 0)
            Console.WriteLine($"  WARNING: no article for {missing.Count} project(s), they will get a skeleton: {string.Join(", ", missing.Take(5))}");

        if (opts.DryRun)
        {
            Console.WriteLine("dry run: no writes performed");
            foreach (var e in manifest)
                Console.WriteLine($"  would {(opts.Overwrite ? "upsert" : "create")} {e.Slug,-34} {e.Category}");
            return 0;
        }

        if (!await ConfirmBlogDeletion(db, mediatr, opts)) return 1;

        var blogIds = new Dictionary<ProjectCategory, Guid>();
        foreach (var definition in ProjectBlogs.All)
        {
            var existing = await db.Blogs.FirstOrDefaultAsync(b => b.Slug == definition.Slug);
            if (existing is not null)
            {
                if (existing.Kind != BlogKind.Project)
                {
                    Console.Error.WriteLine($"'{definition.Slug}' exists but is not a Project blog. Fix it by hand; refusing to guess.");
                    return 1;
                }
                blogIds[definition.Category] = existing.Id;
                Console.WriteLine($"  blog {definition.Slug,-14} already exists");
                continue;
            }

            blogIds[definition.Category] = await mediatr.Send(new CreateBlogCommand(
                definition.Label, definition.Slug, DescriptionFor(definition.Category),
                definition.Icon, BlogKind.Project));
            Console.WriteLine($"  blog {definition.Slug,-14} created");
        }

        var created = 0;
        var skipped = 0;
        var updated = 0;
        foreach (var entry in manifest)
        {
            var blogId = blogIds[entry.Category];
            // A draft that already exists is updated rather than duplicated, so
            // re-seeding refreshes prose and metadata without changing ids.
            // That matters because the first seed wrote skeletons: re-running
            // without this would leave the old bodies in place forever.
            var existing = await db.Posts
                .OfType<Project>()
                .FirstOrDefaultAsync(p => p.BlogId == blogId && p.Slug == entry.Slug);

            if (existing is not null)
            {
                if (!opts.Overwrite)
                {
                    Console.WriteLine($"  skip {entry.Slug,-34} already present (use --overwrite to refresh)");
                    skipped++;
                    continue;
                }

                await mediatr.Send(new UpdateProjectCommand(
                    BlogId: blogId,
                    Id: existing.Id,
                    Title: entry.Title,
                    Slug: entry.Slug,
                    Content: Body(bodies, entry),
                    Description: entry.Description,
                    Category: entry.Category,
                    Year: entry.Year,
                    Stack: entry.Stack,
                    LastPushedAt: entry.LastPushedAt,
                    Links: entry.Links.Select(l => new ProjectLinkInput(l.Label, l.Url)).ToList(),
                    CoverImageUrl: null,
                    IsPublished: existing.IsPublished));
                updated++;
                Console.WriteLine($"  update {entry.Slug,-34} {(bodies.ContainsKey(entry.Slug) ? "article body" : "skeleton")}");
                continue;
            }

            await mediatr.Send(new CreateProjectCommand(
                BlogId: blogId,
                Title: entry.Title,
                Slug: entry.Slug,
                Content: Body(bodies, entry),
                Description: entry.Description,
                Category: entry.Category,
                Year: entry.Year,
                Stack: entry.Stack,
                LastPushedAt: entry.LastPushedAt,
                Links: entry.Links.Select(l => new ProjectLinkInput(l.Label, l.Url)).ToList(),
                CoverImageUrl: null,
                IsPublished: false));
            created++;
            Console.WriteLine($"  draft {entry.Slug,-34} {entry.Category} {entry.Year}");
        }

        Console.WriteLine($"done: {created} created, {updated} updated, {skipped} skipped");
        return 0;
    }

    private static string Body(Dictionary<string, string> bodies, ManifestEntry entry) =>
        bodies.TryGetValue(entry.Slug, out var body) ? body : DraftBody(entry);

    private static async Task<bool> ConfirmBlogDeletion(CmsDbContext db, IMediator mediatr, Options opts)
    {
        if (!opts.DeleteLegacyProjectsBlog) return true;
        var legacy = await db.Blogs.FirstOrDefaultAsync(b => b.Slug == "projects");
        if (legacy is null)
        {
            Console.WriteLine("  legacy 'projects' blog already gone");
            return true;
        }

        if (await db.Posts.AnyAsync(p => p.BlogId == legacy.Id))
        {
            Console.Error.WriteLine("Refusing to delete 'projects': it is not empty. Move its posts first.");
            return false;
        }

        await mediatr.Send(new DeleteBlogCommand(legacy.Id));
        Console.WriteLine("  deleted empty legacy 'projects' blog");
        return true;
    }

    private static string DescriptionFor(ProjectCategory category) => category switch
    {
        ProjectCategory.GameDev => "Games and interactive experiments, mostly built for the joy of building them.",
        ProjectCategory.Graphics => "Visual work: shaders, rendering and generative art.",
        ProjectCategory.BusinessCase => "The unglamorous side: the small tools and systems that make the rest of this possible.",
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
    };

    /// <summary>
    /// A skeleton, not an article. Everything derivable from the repository is
    /// filled in; everything that is a judgement or a memory is left as a visible
    /// TODO so it is obvious what still needs writing.
    /// </summary>
    private static string DraftBody(ManifestEntry e)
    {
        var b = new System.Text.StringBuilder();
        b.AppendLine($"# {e.Title}");
        b.AppendLine();
        b.AppendLine("## What it does");
        b.AppendLine();
        b.AppendLine(string.IsNullOrWhiteSpace(e.GitHubDescription)
            ? "<!-- TODO: describe what this is in a sentence or two. -->"
            : e.GitHubDescription);
        b.AppendLine();
        b.AppendLine("## How it works");
        b.AppendLine();
        b.AppendLine("<!-- TODO: the interesting technical decision, the part that was not obvious. -->");
        b.AppendLine();
        b.AppendLine("## Why I built it");
        b.AppendLine();
        b.AppendLine("<!-- TODO: this is the part only you can write. What made you start it? -->");
        b.AppendLine();
        b.AppendLine("## What I learned");
        b.AppendLine();
        b.AppendLine("<!-- TODO: what surprised you, what you would do differently. -->");
        return b.ToString();
    }
}

/// <summary>
/// Satisfies the R2 dependency without credentials. The only member the project
/// write path touches is <see cref="PublicBucketUrl"/>; every mutating member
/// throws, so a seeding mistake cannot write to or delete from the bucket.
/// </summary>
internal sealed class OfflineR2(string publicBucketUrl) : IR2StorageService
{
    public string PublicBucket => "offline";
    public string BackupBucket => "offline";
    public string PublicBucketUrl => publicBucketUrl;
    public string Environment => "offline";

    private static Exception Refuse(string op) =>
        new InvalidOperationException($"The seeder must not call {op} on R2.");

    public Task UploadAsync(string bucket, string key, Stream content, string contentType, CancellationToken ct = default)
        => throw Refuse(nameof(UploadAsync));

    public Task<Stream> DownloadAsync(string bucket, string key, CancellationToken ct = default)
        => throw Refuse(nameof(DownloadAsync));

    public Task DeleteAsync(string bucket, string key, CancellationToken ct = default)
        => throw Refuse(nameof(DeleteAsync));

    public Task<bool> ObjectExistsAsync(string bucket, string key, CancellationToken ct = default)
        => throw Refuse(nameof(ObjectExistsAsync));
}
