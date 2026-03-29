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
          <ArticleCard
            v-for="post in group.posts"
            :key="post.id"
            :post="post"
            :blog-id="group.blog.id"
            status="draft"
            :is-publishing="articleStore.isPublishing(post.id!)"
            @publish="(p) => handlePublish(p, group.blog.id!)"
          />
        </div>
      </div>
    </template>

    <EmptyState
      v-else
      title="No drafts yet"
      description="All your drafts will appear here, grouped by blog."
      icon="i-lucide-file-text"
      action-label="Go to Blogs"
      action-to="/blogs"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import dayjs from 'dayjs'
import { useToast } from '@nuxt/ui/composables'
import { useArticleStore } from '@/stores/article-store'
import { useBlogStore } from '@/stores/blog-store'
import type { PostResponse } from '@/stores/article-store'
import ArticleCard from '@/components/ArticleCard.vue'
import EmptyState from '@/components/EmptyState.vue'

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
        const dateA = a.createdOn ? dayjs(a.createdOn).valueOf() : 0
        const dateB = b.createdOn ? dayjs(b.createdOn).valueOf() : 0
        return dateB - dateA
      }),
    }))
    .filter((g) => g.blog && g.posts.length > 0)
})

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
