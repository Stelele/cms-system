using Domain.Abstractions;
using Domain.Blogs;
using Domain.Posts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Models;

public class CmsDbContext(DbContextOptions<CmsDbContext> options, IPublisher publisher) : DbContext(options)
{
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Post> Posts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        new BlogEntity().Configure(modelBuilder.Entity<Blog>());
        new PostEntity().Configure(modelBuilder.Entity<Post>());
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
