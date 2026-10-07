# Frontend Development Guide

## Shared conventions

Cross-project decisions, code style, stack choices, testing expectations and workflow live
in the hivemind board. **Read `core/INDEX.md` before coding**, then only the 2-3 core files
it points at for the work in hand.

The board is a private repo. If it is not checked out on this machine yet:

    mkdir -p ~/Documents/code-projects
    git clone git@github.com:Stelele/hivemind.git ~/Documents/code-projects/hivemind

Then either of these updates it — adjust the path if you cloned it elsewhere:

    ~/Documents/code-projects/hivemind/hm pull        # the board CLI
    git -C ~/Documents/code-projects/hivemind pull    # plain git

Board: https://github.com/Stelele/hivemind — 48 ratified rules, each with an ADR
recording why and what it costs.

Everything below is specific to this project.

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

No test framework is currently configured in this project.
