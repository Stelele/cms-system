<template>
  <div class="w-full h-full grid grid-cols-1 gap-4 p-4">
    <UForm :schema="schema" :state="state" class="space-y-4">
      <UFormField
        label="Name"
        name="name"
        :required="true"
        description="Please enter the name of the blog."
      >
        <UInput v-model="state.name" class="w-full" />
      </UFormField>

      <UFormField
        label="Slug"
        name="slug"
        :required="true"
        description="Please enter the slug of the blog."
      >
        <UInput v-model="state.slug" class="w-full" :disabled="mode === 'edit'" />
      </UFormField>

      <UFormField
        label="Description"
        name="description"
        :required="true"
        description="Please enter a description of the blog."
      >
        <UTextarea v-model="state.description" class="w-full" :rows="6" />
      </UFormField>

      <UFormField
        label="Icon"
        name="icon"
        :required="true"
        description="Choose an icon or enter a custom Iconify class."
      >
        <IconPicker v-model="state.icon" />
      </UFormField>

      <UFormField
        label="Kind"
        name="kind"
        description="A project blog holds typed projects with a category, year, stack and links. The API only accepts the slugs game-dev, graphics and business-case for a project blog."
      >
        <USelect v-model="state.kind" :items="kindItems" value-key="value" class="w-full" />
      </UFormField>

      <UAlert
        v-if="state.kind === 'Project' && !isRegisteredProjectSlug"
        color="warning"
        variant="subtle"
        icon="i-lucide-triangle-alert"
        title="This slug is not a project slug"
        :description="projectSlugWarning"
      />

      <div class="w-full flex justify-end">
        <UButton
          type="button"
          :disabled="!isValid"
          :loading="isSubmitting"
          class="hover:cursor-pointer"
          @click="onButtonClick"
        >
          {{ mode === 'edit' ? 'Update Blog' : 'Submit' }}
        </UButton>
      </div>
    </UForm>
  </div>
</template>

<script setup lang="ts">
import * as z from 'zod'
import { computed, reactive, ref, watch, onMounted } from 'vue'
import type { components } from '@/services/backend/schema'
import { useBlogStore } from '@/stores/blog-store'
import { BackendApiSingleton } from '@/services/backend'
import IconPicker from '@/components/IconPicker.vue'

const props = defineProps<{
  mode?: 'create' | 'edit'
  blogId?: string
  initialData?: {
    name: string
    slug: string
    description: string
    icon: string
    kind?: BlogKind
  }
}>()

const blogStore = useBlogStore()
const toast = useToast()

const emit = defineEmits<{
  success: []
}>()

const isSubmitting = ref(false)

const currentBlogName = ref('')
const currentBlogSlug = ref('')

const blogNames = computed(() =>
  blogStore.blogs
    .filter((b) => b.name !== currentBlogName.value)
    .map((blog) => blog.name),
)
const blogSlugs = computed(() =>
  blogStore.blogs
    .filter((b) => b.slug !== currentBlogSlug.value)
    .map((blog) => blog.slug),
)

type BlogKind = components['schemas']['BlogKind']
type CreateBlogCommand = components['schemas']['CreateBlogCommand']
type UpdateBlogCommand = components['schemas']['UpdateBlogCommand']

function buildSchema(blogNamesList: string[], blogSlugsList: string[]) {
  return z.object({
    name: z
      .string()
      .min(4)
      .refine((value) => !blogNamesList.includes(value), {
        message: 'Blog name already exists',
      }),
    slug: z
      .string()
      .min(4)
      .refine((value) => !blogSlugsList.includes(value), {
        message: 'Blog slug already exists',
      }),
    description: z.string(),
    icon: z.string().min(1),
    kind: z.enum(['Standard', 'Project']),
  })
}

type Schema = z.infer<ReturnType<typeof buildSchema>>

const schema = computed(() => buildSchema(blogNames.value, blogSlugs.value))

const state = reactive<Schema>({
  name: '',
  slug: '',
  description: '',
  icon: 'i-heroicons-book-open',
  kind: 'Standard',
})

const isValid = computed(() => schema.value.safeParse(state).success)

const kindItems: Array<{ label: string; value: BlogKind }> = [
  { label: 'Standard - ordinary posts', value: 'Standard' },
  { label: 'Project - typed projects', value: 'Project' },
]

// Mirrors ProjectBlogs on the backend. The API re-checks, so this only saves
// the round trip and explains the rule before the user hits save.
const registeredProjectSlugs = ['game-dev', 'graphics', 'business-case']
const isRegisteredProjectSlug = computed(() => registeredProjectSlugs.includes(state.slug))
const projectSlugWarning = computed(
  () =>
    `The API will refuse to save a project blog called "${state.slug}". ` +
    `Use one of: ${registeredProjectSlugs.join(', ')}.`,
)

onMounted(() => {
  if (props.mode === 'edit' && props.initialData) {
    state.name = props.initialData.name
    state.slug = props.initialData.slug
    state.description = props.initialData.description
    state.icon = props.initialData.icon
    state.kind = props.initialData.kind ?? 'Standard'
    currentBlogName.value = props.initialData.name
    currentBlogSlug.value = props.initialData.slug
  }
})

watch(
  () => props.initialData,
  (data) => {
    if (props.mode === 'edit' && data) {
      state.name = data.name
      state.slug = data.slug
      state.description = data.description
      state.icon = data.icon
      state.kind = data.kind ?? 'Standard'
      currentBlogName.value = data.name
      currentBlogSlug.value = data.slug
    }
  },
  { immediate: true },
)

async function onButtonClick() {
  if (isSubmitting.value || (props.mode === 'edit' && !props.blogId)) return
  isSubmitting.value = true

  const client = await BackendApiSingleton.getInstance()

  if (props.mode === 'edit' && props.blogId) {
    const blogId = props.blogId
    const updateData: UpdateBlogCommand = {
      id: blogId,
      name: state.name,
      description: state.description,
      icon: state.icon,
      kind: state.kind,
    }

    const result = await client.PUT('/blogs/{id}', {
      params: { path: { id: blogId } },
      body: updateData,
    })

    if (result.response.ok) {
      toast.add({ title: 'Blog updated successfully', color: 'success' })
      await blogStore.update()
      emit('success')
      isSubmitting.value = false
      return
    }

    const errorData = result.data as { error?: { message?: string } } | undefined
    toast.add({
      title: 'Failed to update blog',
      description: errorData?.error?.message ?? 'An unexpected error occurred.',
      color: 'error',
      duration: 8000,
    })
  } else {
    const createData: CreateBlogCommand = {
      name: state.name,
      slug: state.slug,
      description: state.description,
      icon: state.icon,
      kind: state.kind,
    }

    const result = await client.POST('/blogs', { body: createData })

    if (result.response.ok) {
      toast.add({ title: 'Blog created successfully', color: 'success' })
      state.name = ''
      state.slug = ''
      state.description = ''
      state.icon = 'i-heroicons-book-open'
      await blogStore.update()
      emit('success')
      isSubmitting.value = false
      return
    }

    const errorData = result.data as { error?: { message?: string } }
    toast.add({
      title: 'Failed to create blog',
      description: errorData?.error?.message ?? 'An unexpected error occurred.',
      color: 'error',
      duration: 8000,
    })
  }

  isSubmitting.value = false
}
</script>
