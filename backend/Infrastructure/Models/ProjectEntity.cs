using System.Text.Json;
using Domain.Posts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Models;

/// <summary>
/// Table-Per-Hierarchy: Project adds a discriminator to Posts rather than
/// creating a table. Stack and Links are value-converted to JSON columns - the
/// serialisation is real, but the shape is enforced by List&lt;string&gt; and
/// List&lt;ProjectLink&gt; at the domain boundary, so nothing arbitrary gets in.
/// </summary>
public class ProjectEntity : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.Property(b => b.Category)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(b => b.Year)
            .IsRequired();

        builder.Property(b => b.LastPushedAt)
            .IsRequired(false);

        builder.Property(b => b.Stack)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>(),
                JsonValueComparer.ForList<string>());

        builder.Property(b => b.Links)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<ProjectLink>>(v, (JsonSerializerOptions?)null) ?? new List<ProjectLink>(),
                JsonValueComparer.ForList<ProjectLink>());

        builder.HasIndex(b => new { b.BlogId, b.Year });
    }
}

/// <summary>
/// Compares value-converted collections by their serialised form, so EF does not
/// see a "new" value on every read and stop persisting edits.
///
/// The snapshot expression is what makes in-place mutation visible: without it EF
/// copies the list by reference and <c>project.Stack.Add("WGSL")</c> would never
/// be detected as a change. Handlers assign a fresh list, which reference
/// equality would catch, but the admin UI may mutate in place, so the snapshot
/// is supplied rather than assumed.
///
/// Note: EF Core's third <c>HasConversion</c> parameter is a
/// <see cref="ValueComparer{T}"/>, not an <c>IEqualityComparer&lt;T&gt;</c>. The
/// plan originally specified <c>System.Text.Json.JsonComparer&lt;T&gt;</c>, which
/// does not exist in System.Text.Json.
/// </summary>
internal static class JsonValueComparer
{
    public static ValueComparer<List<T>> ForList<T>() => new(
        (a, b) => JsonSerializer.Serialize(a, (JsonSerializerOptions?)null)
                 == JsonSerializer.Serialize(b, (JsonSerializerOptions?)null),
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null).GetHashCode(StringComparison.Ordinal),
        // new List<T>() rather than a collection expression: HasConversion takes
        // expression trees, and CS9175 forbids a collection expression inside one.
        v => JsonSerializer.Deserialize<List<T>>(
            JsonSerializer.Serialize(v, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null)
            ?? new List<T>());
}
