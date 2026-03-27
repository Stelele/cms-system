using Domain.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Models;

public class FileEntity : IEntityTypeConfiguration<FileItem>
{
    public void Configure(EntityTypeBuilder<FileItem> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.FileName).IsRequired().HasMaxLength(255);
        builder.Property(f => f.Extension).IsRequired().HasMaxLength(10);
        builder.Property(f => f.ContentType).IsRequired().HasMaxLength(50);
        builder.Property(f => f.Size).IsRequired();
        builder.Property(f => f.StoragePath).IsRequired();
        builder.Property(f => f.Url).IsRequired();
        builder.Property(f => f.ContentHash).IsRequired().IsFixedLength().HasMaxLength(64);
        builder.Property(f => f.AltText).HasMaxLength(500);

        builder.HasIndex(f => f.ContentHash).IsUnique();

        builder.HasMany(f => f.Posts)
               .WithMany(p => p.Files)
               .UsingEntity("PostFiles");
    }
}
