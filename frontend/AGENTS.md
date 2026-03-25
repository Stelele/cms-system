# Frontend Development Guide

## Tech Stack
- **Vue 3** with Composition API (`<script setup lang="ts">`)
- **TypeScript** for type safety
- **Vite** for build tooling
- **Pinia** for state management
- **Vue Router** for routing
- **@nuxt/ui** for UI components
- **openapi-fetch** for API communication
- **Zod** for runtime validation

## Project Structure

```
frontend/src/
├── components/       # Reusable Vue components
├── views/            # Page-level components (route views)
├── stores/           # Pinia stores for shared state
├── services/         # API clients and type schemas
├── router/           # Vue Router configuration
├── layouts/          # Page layout wrappers
└── main.ts           # Application entry point
```

## Commands

```bash
# Install dependencies
npm install

# Development server (http://localhost:5173)
npm run dev

# Production build
npm run build

# Type checking (vue-tsc)
npm run type-check

# Lint (oxlint + eslint)
npm run lint

# Format code with Prettier
npm run format
```

## TypeScript Conventions

### Explicit Types
Always use explicit types for function parameters and return values:
```typescript
// Good
const fetchBlogs = async (): Promise<BlogResponse[]> => {
  const { data } = await client.GET('/blogs')
  return data ?? []
}
```

### Type Imports
Use `import type` for type-only imports to avoid runtime overhead:
```typescript
import type { components } from '@/services/backend/schema'
```

### Zod for Validation
Use Zod schemas for form validation and runtime type checking:
```typescript
import * as z from 'zod'

const schema = z.object({
  name: z.string().min(4),
  slug: z.string().min(4),
  description: z.string(),
})
type Schema = z.output<typeof schema>
```

## Vue Component Conventions

### Component Structure
Vue components should follow this structure (enforced by ESLint):
1. `<template>` - HTML template at the top
2. `<script setup lang="ts">` - Composition API script below template
3. `<style>` - Styles at the very bottom (if needed)

```vue
<template>
  <div class="example">
    {{ message }}
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useBlogStore } from '@/stores/blog-store'

const blogStore = useBlogStore()
const localState = ref('')
</script>

<style scoped>
.example {
  color: red;
}
</style>
```

### Script Setup
Use `<script setup lang="ts">` for all components:
```vue
<script setup lang="ts">
import { ref, computed } from 'vue'
import { useBlogStore } from '@/stores/blog-store'

const blogStore = useBlogStore()
const localState = ref('')
</script>
```

### State Management (Pinia)
Use Pinia stores for shared state. Define stores using the composition API:
```typescript
// stores/blog-store.ts
import { BackendApiSingleton } from '@/services/backend'
import type { components } from '@/services/backend/schema'
import { defineStore } from 'pinia'
import { ref } from 'vue'

export const useBlogStore = defineStore('blogStore', () => {
  const blogs = ref<components['schemas']['BlogResponse'][]>([])

  async function update() {
    const client = await BackendApiSingleton.getInstance()
    const { data } = await client.GET('/blogs')
    blogs.value = data ?? []
  }

  return { blogs, update }
})
```

## API Client Pattern

The codebase uses a singleton pattern for the API client with openapi-fetch.

### Setup (`services/backend/index.ts`)
```typescript
import createClient from 'openapi-fetch'
import type { paths } from './schema'
import { useAuthStore } from '@/stores/auth-store'

export type Client = ReturnType<typeof createClient<paths>>

export class BackendApiSingleton {
  private static instance: Client | null = null

  public static async getInstance() {
    if (this.instance) return this.instance
    const authStore = useAuthStore()

    const api = createClient<paths>({
      baseUrl: import.meta.env.VITE_API_URL,
      headers: {
        Authorization: `Bearer ${authStore.accessToken}`,
        'Content-Type': 'application/json',
        Accept: 'application/json'
      },
    })

    this.instance = api
    return api
  }
}
```

### Usage
```typescript
const client = await BackendApiSingleton.getInstance()
const { data, error } = await client.GET('/blogs')

if (error) {
  // Handle error
  return
}

// Use data
const blogs = data ?? []
```

## Naming Conventions

| Type | Convention | Example |
|------|-----------|---------|
| Components | PascalCase | `NewBlogForm.vue` |
| Stores | camelCase + store suffix | `blog-store.ts`, `useBlogStore` |
| Types | PascalCase | `BlogResponse`, `CreateBlogCommand` |
| Variables | camelCase | `accessToken`, `blogList` |
| Constants | UPPER_SNAKE_CASE | `MAX_RETRY_COUNT` |
| CSS Classes | Tailwind utilities | `class="w-full h-full grid"` |

## Import Order

Always order imports as follows:
1. Vue/Pinia imports (`ref`, `reactive`, `defineStore`)
2. Type imports (`import type`)
3. External library imports (`zod`, `vue-router`)
4. Path alias imports (`@/stores/...`, `@/components/...`)
5. Relative imports (`./utils`, `../types`)

## Vue Router

Use lazy loading for route components:
```typescript
const Home = () => import('@/views/Home.vue')
const Blogs = () => import('@/views/Blogs.vue')

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'home',
    beforeEnter: authGuard,  // Auth0 guard
    component: Home,
  },
]
```

## UI Components

Use @nuxt/ui components throughout. Common patterns:
```vue
<UForm :schema="schema" :state="state">
  <UFormField label="Name" name="name" :required="true">
    <UInput v-model="state.name" class="w-full" />
  </UFormField>
</UForm>

<UButton @click="onSubmit">Submit</UButton>
<UNotification title="Success" color="green" />
```

## Linting & Formatting

### ESLint (eslint.config.ts)
- Uses flat config with TypeScript and Vue support
- oxlint plugin for additional rules
- Prettier for formatting

### Prettier (.prettierrc.json)
```json
{
  "semi": false,
  "singleQuote": true,
  "printWidth": 100
}
```

### EditorConfig (.editorconfig)
- 2-space indentation
- LF line endings
- Max line length: 100

## Environment Variables

Create `.env.local` for local overrides:
```
VITE_API_URL=http://localhost:5000
```

## Testing

No test framework is currently configured. When adding tests:
- Use Vitest for unit tests
- Use Playwright for E2E tests
