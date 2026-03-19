using Domain.Posts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Models;

public class PostEntity : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.BlogId)
            .IsRequired();

        builder.Property(b => b.Title)
            .IsRequired();

        builder.Property(b => b.Slug)
            .IsRequired();

        builder.HasIndex(b => new { b.BlogId, b.Slug })
            .IsUnique();

        builder.Property(b => b.Content)
            .IsRequired();

        builder.Property(b => b.Tag)
            .IsRequired();

        builder.Property(b => b.CoverImageUrl)
            .IsRequired(false);

        builder.Property(b => b.PublishedOn)
            .IsRequired(false);

        builder.Property(b => b.IsPublished)
            .IsRequired();

        builder.Property(b => b.CreatedOn)
            .IsRequired();

        builder.Property(b => b.UpdatedOn)
            .IsRequired();
    }
}
