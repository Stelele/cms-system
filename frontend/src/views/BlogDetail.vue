<template>
  <div>
    <div v-if="isLoading" class="flex justify-center py-16">
      <UProgress />
    </div>

    <template v-else-if="blog">
      <UPageHeader :title="blog.name" :description="blog.description" class="pe-4">
        <template #links>
          <UButton
            label="New Post"
            icon="i-lucide-plus"
            color="primary"
            @click="router.push(`/write?blogId=${blog.id}`)"
          />
        </template>
      </UPageHeader>

      <template v-if="sortedPosts.length">
        <div v-if="draftPosts.length">
          <h2 class="mb-4 px-4 text-xl font-semibold">Drafts</h2>
          <div class="grid grid-cols-1 gap-6 px-4 md:grid-cols-2 lg:grid-cols-3">
            <article
              v-for="post in draftPosts"
              :key="post.id"
              class="group relative flex flex-col overflow-hidden rounded-lg border border-default bg-(--ui-bg-elevated) transition-colors hover:border-primary"
            >
              <div v-if="post.coverImageUrl" class="aspect-video overflow-hidden">
                <img
                  :src="post.coverImageUrl"
                  :alt="post.title ?? ''"
                  class="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
                />
              </div>
              <div class="flex flex-1 flex-col p-4">
                <div class="mb-2 flex items-center justify-between">
                  <UBadge color="neutral" variant="subtle" label="Draft" />
                  <span class="text-xs text-muted">{{ formatDate(post.createdOn) }}</span>
                </div>
                <h3 class="mb-2 text-lg font-semibold leading-tight">
                  <RouterLink
                    :to="`/write?blogId=${blog.id}&edit=${post.id}`"
                    class="hover:text-primary"
                  >
                    {{ post.title }}
                  </RouterLink>
                </h3>
                <p class="mb-4 flex-1 text-sm text-muted line-clamp-3">
                  {{ stripMarkdown(post.content ?? '') }}
                </p>
                <div class="mt-auto flex gap-2">
                  <UButton
                    size="sm"
                    color="primary"
                    :loading="articleStore.isPublishing(post.id!)"
                    :disabled="articleStore.isPublishing(post.id!)"
                    @click="handlePublish(post)"
                  >
                    Publish
                  </UButton>
                  <UButton
                    size="sm"
                    color="neutral"
                    variant="outline"
                    :to="`/write?blogId=${blog.id}&edit=${post.id}`"
                  >
                    Edit
                  </UButton>
                </div>
              </div>
            </article>
          </div>
        </div>

        <div v-if="publishedPosts.length" :class="draftPosts.length ? 'mt-12' : ''">
          <h2 class="mb-4 px-4 text-xl font-semibold">Published</h2>
          <div class="grid grid-cols-1 gap-6 px-4 md:grid-cols-2 lg:grid-cols-3">
            <article
              v-for="post in publishedPosts"
              :key="post.id"
              class="group relative flex flex-col overflow-hidden rounded-lg border border-default bg-(--ui-bg-elevated) transition-colors hover:border-primary"
            >
              <div v-if="post.coverImageUrl" class="aspect-video overflow-hidden">
                <img
                  :src="post.coverImageUrl"
                  :alt="post.title ?? ''"
                  class="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
                />
              </div>
              <div class="flex flex-1 flex-col p-4">
                <div class="mb-2 flex items-center justify-between">
                  <UBadge color="success" variant="subtle" label="Published" />
                  <span class="text-xs text-muted">{{ formatDate(post.publishedOn) }}</span>
                </div>
                <h3 class="mb-2 text-lg font-semibold leading-tight">
                  <RouterLink
                    :to="`/write?blogId=${blog.id}&edit=${post.id}`"
                    class="hover:text-primary"
                  >
                    {{ post.title }}
                  </RouterLink>
                </h3>
                <p class="mb-4 flex-1 text-sm text-muted line-clamp-3">
                  {{ stripMarkdown(post.content ?? '') }}
                </p>
                <div class="mt-auto">
                  <UButton
                    size="sm"
                    color="neutral"
                    variant="outline"
                    :to="`/write?blogId=${blog.id}&edit=${post.id}`"
                  >
                    Edit
                  </UButton>
                </div>
              </div>
            </article>
          </div>
        </div>
      </template>

      <div v-else class="px-4 py-16 text-center">
        <div class="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-(--ui-bg-elevated)">
          <UIcon name="i-lucide-file-text" class="h-8 w-8 text-muted" />
        </div>
        <h2 class="mb-2 text-xl font-semibold">No posts yet</h2>
        <p class="text-muted">Create your first post for this blog.</p>
        <UButton class="mt-6" color="primary" @click="router.push(`/write?blogId=${blog.id}`)">
          Create Post
        </UButton>
      </div>
    </template>

    <div v-else class="p-8 text-center">
      <p class="text-muted">Blog not found</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useToast } from '@nuxt/ui/composables'
import { useBlogStore } from '@/stores/blog-store'
import { useArticleStore } from '@/stores/article-store'
import type { PostResponse } from '@/stores/article-store'

const route = useRoute()
const router = useRouter()
const blogStore = useBlogStore()
const articleStore = useArticleStore()
const toast = useToast()

const isLoading = ref(true)
const allPosts = ref<PostResponse[]>([])

const blog = computed(() => {
  const slug = route.params.slug as string
  return blogStore.blogs.find((b) => b.slug === slug) ?? null
})

const draftPosts = computed(() =>
  allPosts.value
    .filter((p) => !p.isPublished)
    .sort((a, b) => {
      const dateA = a.createdOn ? new Date(a.createdOn).getTime() : 0
      const dateB = b.createdOn ? new Date(b.createdOn).getTime() : 0
      return dateB - dateA
    }),
)

const publishedPosts = computed(() =>
  allPosts.value
    .filter((p) => p.isPublished)
    .sort((a, b) => {
      const dateA = a.publishedOn ? new Date(a.publishedOn).getTime() : 0
      const dateB = b.publishedOn ? new Date(b.publishedOn).getTime() : 0
      return dateB - dateA
    }),
)

const sortedPosts = computed(() => [...draftPosts.value, ...publishedPosts.value])

function stripMarkdown(content: string | undefined | null): string {
  const plainText = (content ?? '')
    .replace(/[#*`_~[\]]/g, '')
    .replace(/\n+/g, ' ')
    .trim()
  return plainText.length > 150 ? plainText.slice(0, 150) + '...' : plainText
}

function formatDate(dateStr: string | undefined | null): string {
  if (!dateStr) return ''
  return new Date(dateStr).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

async function handlePublish(post: PostResponse) {
  if (!blog.value?.id) return
  const success = await articleStore.quickPublish(post)
  if (success) {
    allPosts.value = allPosts.value.map((p) =>
      p.id === post.id ? { ...p, isPublished: true } : p,
    )
    toast.add({
      title: 'Published!',
      description: `"${post.title}" is now live.`,
      color: 'success',
    })
  } else {
    toast.add({
      title: 'Error',
      description: 'Failed to publish the post.',
      color: 'error',
    })
  }
}

onMounted(async () => {
  await blogStore.update()
  if (blog.value?.id) {
    allPosts.value = await articleStore.fetchPostsByBlog(blog.value.id)
  }
  isLoading.value = false
})
</script>
