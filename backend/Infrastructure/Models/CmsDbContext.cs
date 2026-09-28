using Domain.Abstractions;
using Domain.Blogs;
using Domain.Files;
using Domain.Posts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Models;

public class CmsDbContext(DbContextOptions<CmsDbContext> options, IPublisher publisher) : DbContext(options)
{
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<FileItem> FileItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        new BlogEntity().Configure(modelBuilder.Entity<Blog>());
        new PostEntity().Configure(modelBuilder.Entity<Post>());
        new ProjectEntity().Configure(modelBuilder.Entity<Project>());

        // Table-Per-Hierarchy discriminator, configured on the ModelBuilder
        // because EntityTypeBuilder cannot set one.
        //
        // The root value is NAMED. Left to convention, EF emits the column as
        // NOT NULL with defaultValue "", SQLite stamps every pre-existing row
        // with "", and no discriminator matches - the whole Posts table becomes
        // unreadable, which is silent and total.
        //
        // HasValue<Post>(null) is not an option: EF Core 9.0.11's
        // DiscriminatorLengthConvention throws a NullReferenceException while
        // trying to infer a column length from a null root value, whether the
        // discriminator is a name-only shadow property or a real CLR property.
        // Naming the root sidesteps the convention entirely.
        modelBuilder.Entity<Post>()
            .HasDiscriminator<string>("Discriminator")
            .HasValue<Post>("Post");


        new FileEntity().Configure(modelBuilder.Entity<FileItem>());

        if (Database.IsSqlite())
        {
            // Apply converter to all DateTimeOffset and DateTimeOffset? properties
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var properties = entityType.ClrType.GetProperties()
                    .Where(p => p.PropertyType == typeof(DateTimeOffset) 
                            || p.PropertyType == typeof(DateTimeOffset?));
                
                foreach (var property in properties)
                {
                    modelBuilder.Entity(entityType.Name).Property(property.Name)
                        .HasConversion<DateTimeOffsetToBinaryConverter>();
                }
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entities = ChangeTracker.Entries<Base>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var events = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        var modifiedEntries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified || e.State == EntityState.Added);
        foreach (var entry in modifiedEntries)
        {
            if (entry.Entity is Base entity)
            {
                entity.UpdatedOn = DateTime.UtcNow;
            }
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var _event in events)
            await publisher.Publish(_event, cancellationToken);

        foreach (var entity in entities)
            entity.ClearDomainEvents();

        return result;
    }
}
