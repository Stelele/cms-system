# AGENTS.md - CMS System Development Guide

This is a full-stack CMS system with a Vue/TypeScript frontend and .NET backend.

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

#### TypeScript Conventions
- Use explicit types for function parameters and return values
- Prefer `type` over `interface` for simple type aliases
- Use `zod` for runtime validation with OpenAPI-generated types
- Import types explicitly: `import type { Foo } from './foo'`

```typescript
// Good
const fetchBlogs = async (): Promise<BlogResponse[]> => {
  const { data } = await client.GET('/blogs')
  return data ?? []
}

// Avoid
const fetchBlogs = async () => {
  const { data } = await client.GET('/blogs')
  return data ?? []
}
```

#### Vue Component Conventions
- Use `<script setup lang="ts">` for all components
- Import components using path aliases (`@/components/...`)
- Define reactive state using `ref()` or `reactive()`
- Use Pinia stores for shared state management

```vue
<script setup lang="ts">
import { ref, computed } from 'vue'
import { useBlogStore } from '@/stores/blog-store'

const blogStore = useBlogStore()
const localState = ref('')
</script>
```

#### Naming Conventions
- Components: PascalCase (`NewBlogForm.vue`)
- Files/Variables: camelCase (`blogStore.ts`, `accessToken`)
- Constants: UPPER_SNAKE_CASE
- CSS classes: Tailwind utility classes preferred

#### Import Order
1. Vue/Pinia imports
2. Type imports (`import type`)
3. External library imports
4. Internal imports (path aliases)
5. Relative imports

#### API Client Pattern
- Use singleton pattern for API client (`BackendApiSingleton`)
- Leverage openapi-fetch with generated types from `schema.ts`
- Always handle null data cases: `data ?? []`

### Backend (C#)

#### Architecture Pattern
- **CQRS with MediatR**: Commands, Queries, and their Handlers
- **Clean Architecture layers**: Api → Application → Domain/Infrastructure
- **Minimal APIs** for HTTP endpoints

#### Naming Conventions
- Namespaces: PascalCase (`Application.Blogs`)
- Classes: PascalCase (`CreateBlogCommand`, `BlogEndpoints`)
- Records for DTOs and Commands/Queries
- Files match class names (`CreateBlogCommand.cs`)
- Method parameters: camelCase

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

## Key Libraries

| Frontend | Backend |
|----------|---------|
| Vue 3 + Composition API | .NET 10 |
| Pinia (state management) | MediatR (CQRS) |
| Vue Router | FluentValidation |
| @nuxt/ui (components) | Entity Framework Core |
| openapi-fetch | Minimal APIs |
| Zod (validation) | Auth0 JWT |
