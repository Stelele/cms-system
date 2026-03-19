using Domain.Blogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Models;

public class BlogEntity : IEntityTypeConfiguration<Blog>
{
    public void Configure(EntityTypeBuilder<Blog> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
            .IsRequired();

        builder.Property(b => b.Slug)
            .IsRequired();

        builder.HasIndex(b => b.Slug)
            .IsUnique();

        builder.Property(b => b.Description)
            .IsRequired();

        builder.Property(b => b.CreatedOn)
            .IsRequired();

        builder.Property(b => b.UpdatedOn)
            .IsRequired();

        builder.HasMany(b => b.Posts)
            .WithOne(p => p.Blog)
            .HasForeignKey(p => p.BlogId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
