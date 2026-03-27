<template>
  <div>
    <UPageHeader title="Drafts" description="All your unpublished drafts, grouped by blog." class="pe-4" />

    <div v-if="isLoading" class="flex justify-center py-16">
      <UProgress />
    </div>

    <template v-else-if="groupedDrafts.length">
      <div v-for="group in groupedDrafts" :key="group.blog.id" class="mb-12 px-4">
        <h2 class="mb-4 text-xl font-semibold">{{ group.blog.name }}</h2>
        <div class="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
          <article
            v-for="post in group.posts"
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
                <RouterLink :to="`/write?blogId=${group.blog.id}&edit=${post.id}`" class="hover:text-primary">
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
                  @click="handlePublish(post, group.blog.id!)"
                >
                  Publish
                </UButton>
                <UButton
                  size="sm"
                  color="neutral"
                  variant="outline"
                  :to="`/write?blogId=${group.blog.id}&edit=${post.id}`"
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
      <h2 class="mb-2 text-xl font-semibold">No drafts yet</h2>
      <p class="text-muted">All your drafts will appear here, grouped by blog.</p>
      <UButton class="mt-6" color="primary" to="/blogs">
        Go to Blogs
      </UButton>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useToast } from '@nuxt/ui/composables'
import { useArticleStore } from '@/stores/article-store'
import { useBlogStore } from '@/stores/blog-store'
import type { PostResponse } from '@/stores/article-store'

const articleStore = useArticleStore()
const blogStore = useBlogStore()
const toast = useToast()

const draftsByBlog = ref(new Map<string, PostResponse[]>())
const isLoading = ref(true)

const groupedDrafts = computed(() => {
  return [...draftsByBlog.value.entries()]
    .map(([blogId, posts]) => ({
      blog: blogStore.blogs.find((b) => b.id === blogId)!,
      posts: posts.sort((a, b) => {
        const dateA = a.createdOn ? new Date(a.createdOn).getTime() : 0
        const dateB = b.createdOn ? new Date(b.createdOn).getTime() : 0
        return dateB - dateA
      }),
    }))
    .filter((g) => g.blog && g.posts.length > 0)
})

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

  async function handlePublish(post: PostResponse, blogId: string) {
  const success = await articleStore.quickPublish(post)
  if (success) {
    const posts = draftsByBlog.value.get(blogId) ?? []
    const updated = posts.filter((p) => p.id !== post.id)
    if (updated.length === 0) {
      draftsByBlog.value.delete(blogId)
    } else {
      draftsByBlog.value.set(blogId, updated)
    }
    toast.add({ title: 'Published!', description: `"${post.title}" is now live.`, color: 'success' })
  } else {
    toast.add({ title: 'Error', description: 'Failed to publish the post.', color: 'error' })
  }
}

onMounted(async () => {
  draftsByBlog.value = await articleStore.fetchAllDrafts()
  isLoading.value = false
})
</script>
