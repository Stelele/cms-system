# Blog Icon Feature Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add required icon field to blogs with icon picker in frontend

**Architecture:** Add Icon string property to Blog entity, include in Create/Update commands and BlogResponse, create hybrid icon picker component with curated quick picks and custom input

**Tech Stack:** .NET 10, Entity Framework Core, Vue 3, Nuxt UI, Iconify

---

## File Structure

### Backend Files to Modify
- `backend/Domain/Blogs/Blog.cs` - Add Icon property
- `backend/Application/Blogs/CreateBlogCommand.cs` - Add Icon to command and validator
- `backend/Application/Blogs/CreateBlogCommandHandler.cs` - Pass Icon to Blog.Create
- `backend/Application/Blogs/UpdateBlogCommand.cs` - Add Icon to command and validator
- `backend/Application/Blogs/UpdateBlogCommandHandler.cs` - Pass Icon to blog update
- `backend/Application/DTOs/BlogResponse.cs` - Add Icon to response
- `backend/Api/Endpoints/Blogs/BlogEndpoints.cs` - Commands already accept command objects

### Frontend Files to Create
- `frontend/src/components/IconPicker.vue` - Hybrid icon picker component

### Frontend Files to Modify
- `frontend/src/components/NewBlogForm.vue` - Add icon picker field
- `frontend/src/views/Blogs.vue` - Display icon in blog posts
- `frontend/src/views/BlogDetail.vue` - Display icon in blog header

### Infrastructure
- `backend/Infrastructure/Models/` - Create EF Core migration

---

## Task 1: Add Icon to Blog Domain Entity

**Files:**
- Modify: `backend/Domain/Blogs/Blog.cs`

- [ ] **Step 1: Add Icon property to Blog entity**

```csharp
// In backend/Domain/Blogs/Blog.cs, add after Description property:
public string Icon { get; set; } = "i-heroicons-book-open";
```

- [ ] **Step 2: Update Blog.Create factory method**

```csharp
// Update the Create method signature and body:
public static Blog Create(
    string name,
    string slug,
    string description,
    string icon)
{
    return new Blog
    {
        Id = Guid.NewGuid(),
        Name = name,
        Slug = slug,
        Description = description,
        Icon = icon
    };
}
```

- [ ] **Step 3: Commit**

```bash
git add backend/Domain/Blogs/Blog.cs
git commit -m "feat: add Icon property to Blog entity"
```

---

## Task 2: Update CreateBlogCommand

**Files:**
- Modify: `backend/Application/Blogs/CreateBlogCommand.cs`

- [ ] **Step 1: Add Icon to CreateBlogCommand**

```csharp
// In backend/Application/Blogs/CreateBlogCommand.cs, update the record:
public record CreateBlogCommand(
    string Name,
    string Slug,
    string Description,
    string Icon
) : ICommand<Guid>;
```

- [ ] **Step 2: Add Icon validation to validator**

```csharp
// Add to CreateBlogCommandValidator:
RuleFor(x => x.Icon)
    .NotEmpty()
    .Matches(@"^i-[a-z0-9-]+$")
    .WithMessage("Icon must be a valid Iconify class (e.g., i-heroicons-book-open)");
```

- [ ] **Step 3: Commit**

```bash
git add backend/Application/Blogs/CreateBlogCommand.cs
git commit -m "feat: add Icon to CreateBlogCommand with validation"
```

---

## Task 3: Update CreateBlogCommandHandler

**Files:**
- Modify: `backend/Application/Blogs/CreateBlogCommandHandler.cs`

- [ ] **Step 1: Pass Icon to Blog.Create**

```csharp
// In Handle method, change:
var blog = Blog.Create(request.Name, request.Slug, request.Description);
// To:
var blog = Blog.Create(request.Name, request.Slug, request.Description, request.Icon);
```

- [ ] **Step 2: Commit**

```bash
git add backend/Application/Blogs/CreateBlogCommandHandler.cs
git commit -m "feat: pass Icon to Blog.Create in handler"
```

---

## Task 4: Update BlogResponse

**Files:**
- Modify: `backend/Application/DTOs/BlogResponse.cs`

- [ ] **Step 1: Add Icon property to BlogResponse**

```csharp
// Add after Description property:
[JsonPropertyName("icon")]
public string Icon { get; init; } = string.Empty;
```

- [ ] **Step 2: Include Icon in FromDomain**

```csharp
// In FromDomain method, add:
Icon = blog.Icon,
```

- [ ] **Step 3: Commit**

```bash
git add backend/Application/DTOs/BlogResponse.cs
git commit -m "feat: add Icon to BlogResponse"
```

---

## Task 5: Update UpdateBlogCommand

**Files:**
- Modify: `backend/Application/Blogs/UpdateBlogCommand.cs`

- [ ] **Step 1: Add Icon to UpdateBlogCommand**

```csharp
// Update the record:
public record UpdateBlogCommand(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    string Icon
) : ICommand<bool>;
```

- [ ] **Step 2: Add Icon validation**

```csharp
// Add to UpdateBlogCommandValidator:
RuleFor(x => x.Icon)
    .NotEmpty()
    .Matches(@"^i-[a-z0-9-]+$")
    .WithMessage("Icon must be a valid Iconify class (e.g., i-heroicons-book-open)");
```

- [ ] **Step 3: Commit**

```bash
git add backend/Application/Blogs/UpdateBlogCommand.cs
git commit -m "feat: add Icon to UpdateBlogCommand with validation"
```

---

## Task 6: Update UpdateBlogCommandHandler

**Files:**
- Modify: `backend/Application/Blogs/UpdateBlogCommandHandler.cs`

- [ ] **Step 1: Check the current implementation**

```csharp
// Read backend/Application/Blogs/UpdateBlogCommandHandler.cs
```

- [ ] **Step 2: Add Icon update logic**

```csharp
// In Handle method, after updating other properties, add:
blog.Icon = request.Icon;
```

- [ ] **Step 3: Commit**

```bash
git add backend/Application/Blogs/UpdateBlogCommandHandler.cs
git commit -m "feat: add Icon update in UpdateBlogCommandHandler"
```

---

## Task 7: Create EF Core Migration

**Files:**
- Create: `backend/Infrastructure/Infrastructure/Migrations/<timestamp>_AddBlogIcon.cs`
- Modify: `backend/Infrastructure/Models/CmsDbContext.cs` (if needed for model config)

- [ ] **Step 1: Generate migration**

```bash
cd backend
dotnet ef migrations add AddBlogIcon
```

- [ ] **Step 2: Commit**

```bash
git add backend/Infrastructure/Infrastructure/Migrations/
git commit -m "feat: add BlogIcon migration"
```

---

## Task 8: Build Backend and Verify

**Files:**
- Build verification

- [ ] **Step 1: Build the solution**

```bash
cd backend
dotnet build
```

- [ ] **Step 2: If successful, commit**

```bash
git add .
git commit -m "build: verify backend compiles"
```

---

## Task 9: Create IconPicker Component

**Files:**
- Create: `frontend/src/components/IconPicker.vue`

- [ ] **Step 1: Create IconPicker component**

```vue
<template>
  <div class="space-y-2">
    <div class="flex flex-wrap gap-2">
      <UButton
        v-for="icon in quickIcons"
        :key="icon"
        :icon="icon"
        :variant="modelValue === icon ? 'solid' : 'outline'"
        :color="modelValue === icon ? 'primary' : 'gray'"
        size="sm"
        @click="$emit('update:modelValue', icon)"
      />
    </div>
    <UInput
      :model-value="modelValue"
      placeholder="i-heroicons-book-open"
      @update:model-value="$emit('update:modelValue', $event)"
    >
      <template #leading>
        <UIcon :name="modelValue" class="text-lg" />
      </template>
    </UInput>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  modelValue: string
}>()

defineEmits<{
  'update:modelValue': [value: string]
}>()

const quickIcons = [
  'i-heroicons-book-open',
  'i-heroicons-document-text',
  'i-heroicons-chat-bubble-left',
  'i-heroicons-photo',
  'i-heroicons-video-camera',
  'i-heroicons-music-note',
  'i-heroicons-code-bracket',
  'i-lucide-zap',
  'i-mdi-language-typescript',
  'i-mdi-language-javascript',
  'i-mdi-language-python',
  'i-mdi-language-cpp',
]
</script>
```

- [ ] **Step 2: Commit**

```bash
git add frontend/src/components/IconPicker.vue
git commit -m "feat: add IconPicker component"
```

---

## Task 10: Update NewBlogForm

**Files:**
- Modify: `frontend/src/components/NewBlogForm.vue`

- [ ] **Step 1: Add import for IconPicker**

```typescript
// Add after existing imports:
import IconPicker from '@/components/IconPicker.vue'
```

- [ ] **Step 2: Add Icon field to state and schema**

```typescript
// In the schema z.object, add:
icon: z.string().min(1),

// In state reactive, add:
icon: 'i-heroicons-book-open',
```

- [ ] **Step 3: Add IconPicker to template**

```vue
<!-- Add after Description field -->
<UFormField
  label="Icon"
  name="icon"
  :required="true"
  description="Choose an icon or enter a custom Iconify class."
>
  <IconPicker v-model="state.icon" />
</UFormField>
```

- [ ] **Step 4: Include Icon in API call**

```typescript
// In the expense object:
const expense: CreateBlogCommand = {
  name: state.name,
  slug: state.slug,
  description: state.description,
  icon: state.icon,
}
```

- [ ] **Step 5: Reset icon on success**

```typescript
// In success handler, add:
state.icon = 'i-heroicons-book-open'
```

- [ ] **Step 6: Commit**

```bash
git add frontend/src/components/NewBlogForm.vue
git commit -m "feat: add IconPicker to NewBlogForm"
```

---

## Task 11: Update BlogDetail View

**Files:**
- Modify: `frontend/src/views/BlogDetail.vue`

- [ ] **Step 1: Add icon to page header**

```vue
<!-- In UPageHeader, add icon slot or modify to show icon -->
<UPageHeader :title="blog.name" :description="blog.description" class="pe-4">
  <template #icon>
    <UIcon :name="blog.icon" class="text-2xl" />
  </template>
```

- [ ] **Step 2: Commit**

```bash
git add frontend/src/views/BlogDetail.vue
git commit -m "feat: display blog icon in BlogDetail"
```

---

## Task 12: Update Blogs List View

**Files:**
- Modify: `frontend/src/views/Blogs.vue`

- [ ] **Step 1: Add icon to blog post props**

```typescript
// In the posts computed mapping, add icon:
const posts = computed<BlogPostProps[]>(() =>
  blogStore.blogs.map(
    (blog) =>
      ({
        title: blog.name,
        description: blog.description,
        to: `/blog/${blog.slug}`,
        icon: blog.icon,
      }) as BlogPostProps,
  ),
)
```

- [ ] **Step 2: Commit**

```bash
git add frontend/src/views/Blogs.vue
git commit -m "feat: display blog icon in blog list"
```

---

## Task 13: Regenerate OpenAPI Schema

**Files:**
- Modify: `frontend/src/services/backend/schema.ts`

- [ ] **Step 1: Regenerate the schema**

```bash
cd backend
dotnet run --project Host/Host.csproj &
# Wait for API to start, then from another terminal:
cd frontend
npx openapi-typescript http://localhost:5000/openapi/v1.json --output src/services/backend/schema.ts
```

- [ ] **Step 2: Verify BlogResponse now has icon field**

- [ ] **Step 3: Commit**

```bash
git add frontend/src/services/backend/schema.ts
git commit -m "feat: regenerate OpenAPI schema with Icon field"
```

---

## Task 14: Run Lint and Typecheck

**Files:**
- Verification

- [ ] **Step 1: Run frontend type-check**

```bash
cd frontend
npm run type-check
```

- [ ] **Step 2: Run frontend lint**

```bash
cd frontend
npm run lint
```

- [ ] **Step 3: If issues, fix and commit**

- [ ] **Step 4: Final commit**

```bash
git add .
git commit -m "feat: complete blog icon feature - backend and frontend"
```

---

## Plan Complete

All tasks listed above. Execute using subagent-driven-development or executing-plans skill.