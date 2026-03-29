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
            <ArticleCard
              v-for="post in draftPosts"
              :key="post.id"
              :post="post"
              :blog-id="blog.id"
              status="draft"
              :is-publishing="articleStore.isPublishing(post.id!)"
              @publish="handlePublish"
            />
          </div>
        </div>

        <div v-if="publishedPosts.length" :class="draftPosts.length ? 'mt-12' : ''">
          <h2 class="mb-4 px-4 text-xl font-semibold">Published</h2>
          <div class="grid grid-cols-1 gap-6 px-4 md:grid-cols-2 lg:grid-cols-3">
            <ArticleCard
              v-for="post in publishedPosts"
              :key="post.id"
              :post="post"
              :blog-id="blog.id"
              status="published"
            />
          </div>
        </div>
      </template>

      <EmptyState
        v-else
        title="No posts yet"
        description="Create your first post for this blog."
        icon="i-lucide-file-text"
        action-label="Create Post"
        @action="router.push(`/write?blogId=${blog.id}`)"
      />
    </template>

    <div v-else class="p-8 text-center">
      <p class="text-muted">Blog not found</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import dayjs from 'dayjs'
import { useRoute, useRouter } from 'vue-router'
import { useToast } from '@nuxt/ui/composables'
import { useBlogStore } from '@/stores/blog-store'
import { useArticleStore } from '@/stores/article-store'
import type { PostResponse } from '@/stores/article-store'
import ArticleCard from '@/components/ArticleCard.vue'
import EmptyState from '@/components/EmptyState.vue'

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
      const dateA = a.createdOn ? dayjs(a.createdOn).valueOf() : 0
      const dateB = b.createdOn ? dayjs(b.createdOn).valueOf() : 0
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
