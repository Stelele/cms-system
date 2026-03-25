# Backend Development Guide

## Tech Stack
- **.NET 10** Minimal APIs
- **MediatR** for CQRS pattern
- **FluentValidation** for input validation
- **Entity Framework Core** for data access
- **Auth0** for JWT authentication

## Project Structure

```
backend/
├── Api/                    # HTTP layer (endpoints, middleware)
│   ├── Endpoints/         # Minimal API endpoint definitions
│   ├── Authentication/    # Auth0/JWT handlers
│   ├── Permissions.cs     # Authorization permissions
│   └── Tags.cs            # OpenAPI tags
├── Application/           # Business logic (CQRS)
│   ├── Blogs/            # Blog commands, queries, handlers
│   ├── Posts/            # Post commands, queries, handlers
│   ├── DTOs/             # Response DTOs
│   ├── Abstractions/      # Base interfaces (ICommand, IQuery)
│   └── PipelineBehaviours/ # MediatR pipeline (validation)
├── Domain/                 # Domain entities
│   ├── Blogs/
│   └── Posts/
├── Infrastructure/         # Data access, EF Core
│   └── Models/            # DbContext and entities
├── Host/                   # Application entry point
│   └── Middleware/        # GlobalExceptionMiddleware
└── Shared/                 # Shared contracts
```

## Commands

```bash
# Restore NuGet packages
dotnet restore

# Build the solution
dotnet build

# Run the API (Host project)
dotnet run --project Host/Host.csproj

# Run tests (when added)
dotnet test

# Run a specific test
dotnet test --filter "FullyQualifiedName~TestClassName"
```

## Architecture Pattern: CQRS with MediatR

### Commands
Commands represent write operations. They return a result (often an ID).

```csharp
// Commands should be records
public record CreateBlogCommand(
    string Name,
    string Slug,
    string Description
) : ICommand<Guid>;  // ICommand<T> where T is the return type
```

### Queries
Queries represent read operations. They return data.

```csharp
public record GetBlogByIdQuery(Guid Id) : IQuery<BlogResponse?>;

// Response is a DTO, not an entity
public record GetBlogByIdQueryHandler(CmsDbContext db) : IQueryHandler<GetBlogByIdQuery, BlogResponse?>
{
    public async Task<BlogResponse?> Handle(GetBlogByIdQuery request, CancellationToken cancellationToken)
    {
        var blog = await db.Blogs.FindAsync([request.Id], cancellationToken);
        return blog is null ? null : new BlogResponse(blog.Id, blog.Name, blog.Slug, blog.Description);
    }
}
```

## Command/Query Handler Pattern

### ICommand / IQuery Interfaces
Located in `Application/Abstractions/`:
```csharp
public interface ICommand<TResponse> : IRequest<TResponse>;
public interface ICommandHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
    where TRequest : ICommand<TResponse>;

public interface IQuery<TResponse> : IRequest<TResponse>;
public interface IQueryHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
    where TRequest : IQuery<TResponse>;
```

### Handler Implementation
Use primary constructors for dependency injection:
```csharp
public class CreateBlogCommandHandler(CmsDbContext db) : ICommandHandler<CreateBlogCommand, Guid>
{
    public async Task<Guid> Handle(CreateBlogCommand request, CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

## Validation

Use FluentValidation for input validation:
```csharp
public sealed class CreateBlogCommandValidator : AbstractValidator<CreateBlogCommand>
{
    public CreateBlogCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .Matches("^[a-z0-9-]+$")
            .WithMessage("Slug must contain only lowercase letters, numbers, and hyphens.");

        RuleFor(x => x.Description)
            .NotEmpty();
    }
}
```

Validation is automatically applied via `ValidationBehaviour` pipeline behavior.

## Minimal API Endpoints

### Endpoint Convention
Use extension methods on `WebApplication`:
```csharp
public static class BlogEndpoints
{
    public static WebApplication MapBlogsEndpoints(this WebApplication app)
    {
        // Map routes here
        return app;
    }
}
```

### Route Definitions
```csharp
app.MapGet("/blogs", async (IMediator mediator) =>
{
    var blogs = await mediator.Send(new GetBlogsQuery());
    return Results.Ok(blogs);
})
.WithName("GetBlogs")
.WithDisplayName("GetBlogs")
.Produces<List<BlogResponse>>(StatusCodes.Status200OK)
.WithTags(EndpointTags.Blogs)
.RequireAuthorization(Permissions.ReadBlogs);
```

### HTTP Status Codes
| Code | Use Case |
|------|----------|
| 200 | Success |
| 201 | Created (with Location header) |
| 204 | No Content (delete) |
| 400 | Validation errors, bad requests |
| 401 | Unauthorized |
| 404 | Not found |
| 500 | Internal server error |

## Authorization

### Permissions
Define permissions in `Api/Permissions.cs`:
```csharp
public static class Permissions
{
    public const string ReadBlogs = "blogs:read";
    public const string WriteBlogs = "blogs:write";
}
```

### Protecting Endpoints
```csharp
.RequireAuthorization(Permissions.WriteBlogs)
```

## Error Handling

### GlobalExceptionMiddleware
Located in `Host/Middleware/GlobalExceptionMiddleware.cs`. Handles:
- `ValidationException` → 400
- `InvalidOperationException` → 400
- `KeyNotFoundException` → 404
- `UnauthorizedAccessException` → 401
- Other exceptions → 500

### Response Format
```json
{
  "error": {
    "message": "Validation failed",
    "type": "ValidationException",
    "errors": [
      { "property": "Slug", "message": "Slug must contain only lowercase letters..." }
    ]
  }
}
```

## Entity Framework Core

### DbContext
Located in `Infrastructure/Models/CmsDbContext.cs`:
```csharp
public class CmsDbContext : DbContext
{
    public DbSet<Blog> Blogs => Set<Blog>();
    public DbSet<Post> Posts => Set<Post>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure entities
    }
}
```

### Domain Entities
```csharp
public class Blog : Base  // Base provides Id (Guid)
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<Post> Posts { get; set; } = [];

    public static Blog Create(string name, string slug, string description)
    {
        return new Blog
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            Description = description
        };
    }
}
```

## Dependency Injection

Register services in layer-specific `DependencyInjection.cs` files:

```csharp
// Application/DependencyInjection.cs
public static class ApplicationServiceExtensions
{
    public static WebApplicationBuilder AddApplication(this WebApplicationBuilder builder)
    {
        builder.Services.AddMediatR(cfg => 
            cfg.RegisterServicesFromAssembly(typeof(CreateBlogCommand).Assembly));
        
        builder.Services.AddValidatorsFromAssemblyContaining<CreateBlogCommand>();
        
        builder.Services.AddPipelineBehaviours();
        
        return builder;
    }
}
```

## Naming Conventions

| Type | Convention | Example |
|------|-----------|---------|
| Namespaces | PascalCase | `Application.Blogs` |
| Classes | PascalCase | `CreateBlogCommand`, `BlogEndpoints` |
| Records | PascalCase | `CreateBlogCommand`, `BlogResponse` |
| Methods | PascalCase | `Handle`, `MapBlogsEndpoints` |
| Parameters | camelCase | `request`, `cancellationToken` |
| Files | Match class name | `CreateBlogCommand.cs` |

## Project References

Layers reference downward only:
- `Host` → `Api`, `Application`, `Infrastructure`
- `Api` → `Application`
- `Application` → `Domain`, `Infrastructure`
- `Domain` → (no dependencies)
- `Infrastructure` → `Domain`

## OpenAPI/Scalar

API documentation is available at `/scalar` when running in development.
