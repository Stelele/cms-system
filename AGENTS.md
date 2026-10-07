# AGENTS.md - CMS System Development Guide

This is a full-stack CMS system with a Vue/TypeScript frontend and .NET backend.

## Shared conventions

Cross-project decisions, code style, stack choices, testing expectations and workflow live
in the hivemind board. **Read `core/INDEX.md` before coding**, then only the 2-3 core files
it points at for the work in hand.

The board is a private repo. If it is not checked out on this machine yet:

    git clone git@github.com:Stelele/hivemind.git ~/Documents/code-projects/hivemind

Then either of these updates it — adjust the path if you cloned it elsewhere:

    ~/Documents/code-projects/hivemind/hm pull        # the board CLI
    git -C ~/Documents/code-projects/hivemind pull    # plain git

Board: https://github.com/Stelele/hivemind — 48 ratified rules, each with an ADR
recording why and what it costs.

Everything below is specific to this project.

## Project Structure

```
cms-system/
├── frontend/          # Vue 3 + TypeScript + Vite application
│   ├── src/
│   │   ├── components/    # Vue components
│   │   ├── views/         # Page views
│   │   ├── stores/        # Pinia stores
│   │   ├── services/     # API clients and schemas
│   │   ├── router/        # Vue Router configuration
│   │   └── layouts/       # Page layouts
│   ├── eslint.config.ts   # ESLint + oxlint configuration
│   ├── vite.config.ts     # Vite configuration
│   └── package.json
├── backend/           # .NET 10 Minimal API application
│   ├── Api/           # Endpoints and HTTP layer
│   ├── Application/   # Commands, queries, handlers (MediatR pattern)
│   ├── Domain/       # Domain entities and business logic
│   ├── Infrastructure/ # Database, EF Core
│   └── Host/         # Entry point and middleware
```

## IMPORTANT: Port 5173

The frontend MUST run on **port 5173 only**. Auth0 authentication is configured to accept callbacks only on this port. Using any other port will cause login failures.

- If port 5173 is in use, the dev server is already running - just open the browser
- Do NOT specify alternative ports with `npm run dev -- --port XXXX`

## Build Commands

### Frontend (Node.js >= 20.19.0)

```bash
cd frontend

# Install dependencies
npm install

# Development server
npm run dev

# Production build
npm run build

# Type checking
npm run type-check

# Lint (oxlint + eslint)
npm run lint

# Format code
npm run format
```

### Backend (.NET 10)

```bash
cd backend

# Restore packages
dotnet restore

# Build
dotnet build

# Run
dotnet run --project Host/Host.csproj

# Run tests
dotnet test
```

## Code Style Guidelines

### Frontend (Vue/TypeScript)

#### API Client Pattern
The real client — `BackendApiSingleton` — is documented in `frontend/AGENTS.md`; go there
for the implementation instead of a generic description here. The underlying rule is board
ADR 0039.

### Backend (C#)

#### Architecture Pattern
- **Clean Architecture layers**: Api → Application → Domain/Infrastructure

#### Project Conventions
```csharp
// Commands are records with validation
public record CreateBlogCommand(
    string Name,
    string Slug,
    string Description
) : ICommand<Guid>;

// Validators using FluentValidation
public sealed class CreateBlogCommandValidator : AbstractValidator<CreateBlogCommand>
{
    public CreateBlogCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Slug).Matches("^[a-z0-9-]+$");
    }
}

// Handlers with dependency injection
public class CreateBlogCommandHandler(CmsDbContext db) : ICommandHandler<CreateBlogCommand, Guid>
{
    public async Task<Guid> Handle(CreateBlogCommand request, CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

#### Endpoint Conventions
- Use `MapBlogsEndpoints` extension methods
- Configure response types with `.Produces<T>()`
- Use `.RequireAuthorization()` for permissions
- Return appropriate HTTP status codes

#### Dependency Injection
- Register services in layer-specific `DependencyInjection.cs` files
- Use constructor injection (primary constructor preferred)
- All layers reference `Application` for abstractions

## Error Handling

### Frontend
- Use try-catch for async operations
- Handle API errors gracefully with user feedback
- Use Zod for form validation

### Backend
- `GlobalExceptionMiddleware` handles all unhandled exceptions
- FluentValidation for input validation
- Return proper HTTP status codes:
  - 200: Success
  - 201: Created
  - 204: No Content (delete)
  - 400: Validation errors, bad requests
  - 401: Unauthorized
  - 404: Not found
  - 500: Internal server error

## Environment Variables

### Frontend
- `VITE_API_URL`: Backend API base URL

### Backend
- Configure in `Host` project (database, auth, etc.)

### Backend — required in Production

`CorsOriginPolicy.Resolve` runs at startup and **refuses to boot** rather than
serving a permissive policy. A Production deploy missing these will crash on
start, not silently serve every origin.

| Variable | Example | Notes |
|----------|---------|-------|
| `Cors__AllowedOrigins__0` | `https://giftmugweni.com` | The public site, which reads published posts without a token |
| `Cors__AllowedOrigins__1` | `https://stelele.github.io` | The admin UI. It does browser-side writes, so omitting this breaks login |

Array indices continue (`__2`, `__3`, …). Two rules the guard enforces:

- **A wildcard is rejected in every environment**, including Development. No
  legitimate configuration needs one.
- **An empty list is rejected only in Production**, so a developer machine with
  no configuration still starts. Development gets `http://localhost:5173` from
  `appsettings.Development.json` instead.

A wildcard would let any site on the internet read published content through the
anonymous endpoints, which is why it is refused outright rather than warned about.

### Running in Production locally

`Host/Properties/launchSettings.json` hard-sets
`ASPNETCORE_ENVIRONMENT=Development` and **overrides the shell variable**, so:

```bash
# this silently runs Development
ASPNETCORE_ENVIRONMENT=Production dotnet run --project Host/Host.csproj

# this is correct
ASPNETCORE_ENVIRONMENT=Production dotnet run --project Host/Host.csproj --no-launch-profile
```

Without `--no-launch-profile` the environment defaults to Production whenever the
launch profile is bypassed, and the CORS guard fires on an empty allowlist.
