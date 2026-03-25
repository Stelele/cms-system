<template>
  <div class="container mx-auto max-w-5xl px-4 py-8">
    <div class="mb-8 flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold">{{ isEditing ? 'Edit Article' : 'Write New Article' }}</h1>
        <p class="text-muted">Create compelling content for your blog</p>
      </div>
      <div class="flex gap-3">
        <UButton
          color="neutral"
          variant="outline"
          :loading="isSaving"
          :disabled="!canSave || isPublishing"
          @click="savePost(false)"
        >
          Save Draft
        </UButton>
        <UButton
          color="primary"
          :loading="isPublishing"
          :disabled="!canSave || isSaving"
          @click="savePost(true)"
        >
          Publish
        </UButton>
      </div>
    </div>

    <div class="space-y-6">
      <UFormField label="Blog" name="blogId" :required="true">
        <USelectMenu
          v-model="state.blogId"
          :items="blogOptions"
          placeholder="Select a blog"
          class="w-full"
          :disabled="isEditing"
        />
      </UFormField>

      <div class="grid grid-cols-1 gap-6 md:grid-cols-2">
        <UFormField label="Title" name="title" :required="true">
          <UInput
            v-model="state.title"
            placeholder="Enter article title"
            class="w-full"
          />
        </UFormField>

        <UFormField label="Slug" name="slug" :required="true">
          <UInput
            v-model="state.slug"
            placeholder="article-slug"
            class="w-full"
          />
        </UFormField>
      </div>

      <div class="grid grid-cols-1 gap-6 md:grid-cols-2">
        <UFormField label="Tag" name="tag">
          <UInput
            v-model="state.tag"
            placeholder="Category or tag"
            class="w-full"
          />
        </UFormField>

        <UFormField label="Cover Image URL" name="coverImageUrl">
          <UInput
            v-model="state.coverImageUrl"
            placeholder="https://example.com/image.jpg"
            class="w-full"
          />
        </UFormField>
      </div>

      <div>
        <label class="mb-2 block text-sm font-medium">Content</label>
        <div class="overflow-hidden rounded-md border border-default">
          <UEditor
            v-slot="{ editor }"
            v-model="editorContent"
            content-type="markdown"
            :placeholder="{ placeholder: 'Start writing your article...', mode: 'firstLine' }"
            :image="true"
            class="min-h-[500px]"
            @update="handleEditorUpdate"
          >
            <div class="border-b border-default px-3 py-2">
              <div class="flex items-center justify-between">
                <UEditorToolbar :editor="editor" :items="toolbarItems" />
                <UPopover :open="isImageModalOpen" @update:open="isImageModalOpen = $event" class="w-96">
                  <UButton
                    variant="ghost"
                    size="sm"
                    icon="i-lucide-image"
                    @click.stop="openImageModal(editor)"
                  >
                    Insert Image
                  </UButton>
                  <template #content>
                    <div class="p-4 space-y-4">
                      <h2 class="text-lg font-semibold">Insert Image</h2>
                      <p class="text-sm text-muted">Enter an image URL or select a local file</p>
                      <div class="space-y-4">
                        <UFormField label="Image URL" name="imageUrl">
                          <UInput
                            v-model="localImageUrl"
                            placeholder="https://example.com/image.jpg"
                            class="w-full"
                          />
                        </UFormField>

                        <UFormField label="Or Upload File" name="fileUpload">
                          <UInput
                            type="file"
                            accept="image/*"
                            class="w-full"
                            @change="handleFileSelect"
                          />
                        </UFormField>

                        <div v-if="selectedFile" class="flex items-center gap-4">
                          <img
                            :src="getFilePreviewUrl(selectedFile)"
                            alt="Preview"
                            class="h-20 w-20 rounded-md object-cover"
                          />
                          <span class="text-sm text-muted">{{ selectedFile.name }}</span>
                        </div>
                      </div>
                      <div class="flex justify-end gap-3 mt-4">
                        <UButton color="neutral" variant="outline" size="sm" @click="closeModal">
                          Cancel
                        </UButton>
                        <UButton
                          v-if="localImageUrl"
                          :loading="isUploading"
                          size="sm"
                          @click="insertImageUrl"
                        >
                          Insert from URL
                        </UButton>
                        <UButton
                          v-if="selectedFile"
                          :loading="isUploading"
                          size="sm"
                          @click="insertImageFile"
                        >
                          Insert File
                        </UButton>
                      </div>
                    </div>
                  </template>
                </UPopover>
              </div>
            </div>
          </UEditor>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, reactive } from 'vue'
import type { Editor } from '@tiptap/vue-3'
import { useRoute, useRouter } from 'vue-router'
import { useToast } from '@nuxt/ui/composables'
import * as z from 'zod'
import { useBlogStore } from '@/stores/blog-store'
import { useArticleStore } from '@/stores/article-store'
import type { PostResponse } from '@/stores/article-store'
import { useImageInsert } from '@/composables/useImageInsert'
import type { SelectMenuItem } from '@nuxt/ui/runtime/components/SelectMenu.vue.js'

const route = useRoute()
const router = useRouter()
const toast = useToast()
const blogStore = useBlogStore()
const articleStore = useArticleStore()

const {
  isImageModalOpen,
  selectedFile,
  isUploading,
  openImageModal,
  insertImageUrl: insertImageUrlFromModal,
  handleFileSelect,
  insertImageFile,
  closeModal,
} = useImageInsert()

const localImageUrl = ref('')

const insertImageUrl = () => {
  if (localImageUrl.value) {
    insertImageUrlFromModal(localImageUrl.value)
  }
}

const isEditing = computed(() => !!route.query.edit)
const existingPost = ref<PostResponse | null>(null)
const editorContent = ref('')

const schema = z.object({
  blogId: z.refine((blog: any) => blogStore.blogs.some((b) => b.id === blog.id), 'Please select a blog'),
  title: z.string().min(3, 'Title must be at least 3 characters'),
  slug: z
    .string()
    .min(3, 'Slug must be at least 3 characters')
    .regex(/^[a-z0-9-]+$/, 'Slug can only contain lowercase letters, numbers, and dashes'),
  tag: z.string().optional(),
  coverImageUrl: z.string().url('Must be a valid URL').optional().or(z.literal('')),
})

type Schema = z.output<typeof schema>

const state = reactive<Schema>({
  blogId: '',
  title: '',
  slug: '',
  tag: '',
  coverImageUrl: '',
})

const isSaving = ref(false)
const isPublishing = ref(false)

const generateSlug = (text: string): string => {
  return text
    .toLowerCase()
    .replace(/[^a-z0-9\s-]/g, '')
    .replace(/\s+/g, '-')
    .replace(/-+/g, '-')
    .trim()
}

watch(
  () => state.title,
  (newTitle) => {
    if (!isEditing.value && newTitle) {
      state.slug = generateSlug(newTitle)
    }
  },
)

const blogOptions = computed<SelectMenuItem[]>(() =>
  blogStore.blogs.map((blog) => ({
    label: blog.name,
    id: blog.id,
  } as SelectMenuItem)),
)

const toolbarItems = [
  [
    { kind: 'mark', mark: 'bold', icon: 'i-lucide-bold' },
    { kind: 'mark', mark: 'italic', icon: 'i-lucide-italic' },
    { kind: 'mark', mark: 'strike', icon: 'i-lucide-strikethrough' },
    { kind: 'mark', mark: 'code', icon: 'i-lucide-code' },
  ],
  [
    { kind: 'heading', level: 1, icon: 'i-lucide-heading-1' },
    { kind: 'heading', level: 2, icon: 'i-lucide-heading-2' },
    { kind: 'heading', level: 3, icon: 'i-lucide-heading-3' },
  ],
  [
    { kind: 'textAlign', align: 'left', icon: 'i-lucide-align-left' },
    { kind: 'textAlign', align: 'center', icon: 'i-lucide-align-center' },
    { kind: 'textAlign', align: 'right', icon: 'i-lucide-align-right' },
    { kind: 'textAlign', align: 'justify', icon: 'i-lucide-align-justify' },
  ],
  [
    { kind: 'bulletList', icon: 'i-lucide-list' },
    { kind: 'orderedList', icon: 'i-lucide-list-ordered' },
    { kind: 'blockquote', icon: 'i-lucide-quote' },
    { kind: 'codeBlock', icon: 'i-lucide-file-code' },
  ],
  [
    { kind: 'link', icon: 'i-lucide-link' },
    { kind: 'horizontalRule', icon: 'i-lucide-minus' },
  ],
  [
    { kind: 'undo', icon: 'i-lucide-undo' },
    { kind: 'redo', icon: 'i-lucide-redo' },
    { kind: 'clearFormatting', icon: 'i-lucide-remove-formatting' },
  ],
]

const selectedBlogName = computed(() => {
  const blog = blogStore.blogs.find((b) => b.id === state.blogId)
  return blog?.name ?? ''
})

const canSave = computed(() => {
  return state.blogId && state.title && state.slug
})

async function savePost(isPublished: boolean) {
  const validation = schema.safeParse(state)
  console.log({validation, state})
  if (!validation.success) {
    const path = validation.error.issues[0]?.path.join('.')
    const message = validation.error.issues[0]?.message ?? 'Please check your input'
    toast.add({
      title: 'Validation Error',
      description: `${path ? path + ': ' : ''}${message}`,
      color: 'error',
    })
    return
  }

  if (!editorContent.value || editorContent.value === '<p></p>') {
    toast.add({
      title: 'Content Required',
      description: 'Please write some content for your article',
      color: 'error',
    })
    return
  }

  if (isPublished) {
    isPublishing.value = true
  } else {
    isSaving.value = true
  }

  try {
    const postData = {
      blogId: state.blogId,
      title: state.title,
      slug: state.slug,
      content: editorContent.value,
      tag: state.tag || '',
      coverImageUrl: state.coverImageUrl || null,
      isPublished,
    }

    let success = false
    let postId: string | null = null

    if (isEditing.value && existingPost.value) {
      const updateData = {
        ...postData,
        id: existingPost.value.id!,
      }
      success = await articleStore.updatePost(state.blogId, existingPost.value.id!, updateData)
    } else {
      postId = await articleStore.createPost(state.blogId, postData)
      success = !!postId
    }

    if (success) {
      toast.add({
        title: isPublished ? 'Published!' : 'Draft Saved!',
        description: isPublished
          ? 'Your article has been published successfully.'
          : 'Your draft has been saved.',
        color: 'success',
      })

      if (!isEditing.value && !isPublished) {
        const newPostId = postId
        if (newPostId) {
          router.replace({ query: { edit: newPostId } })
        }
      }

      if (isPublished) {
        router.push(`/blog/${selectedBlogName.value}`)
      }
    } else {
      toast.add({
        title: 'Error',
        description: 'Failed to save your article. Please try again.',
        color: 'error',
      })
    }
  } catch {
    toast.add({
      title: 'Error',
      description: 'An unexpected error occurred.',
      color: 'error',
    })
  } finally {
    isSaving.value = false
    isPublishing.value = false
  }
}

function handleEditorUpdate({ editor }: { editor: Editor }) {
  editorContent.value = editor.getMarkdown()
}

onMounted(async () => {
  await blogStore.update()

  if (isEditing.value) {
    const post = articleStore.currentPost

    if (post) {
      existingPost.value = post
      state.blogId = post.blogId ?? ''
      state.title = post.title ?? ''
      state.slug = post.slug ?? ''
      state.tag = post.tag ?? ''
      state.coverImageUrl = post.coverImageUrl ?? ''
      editorContent.value = post.content ?? ''
    }
  }
})

function getFilePreviewUrl(file: File): string {
  return URL.createObjectURL(file)
}
</script>
