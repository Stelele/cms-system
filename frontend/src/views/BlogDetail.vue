<template>
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

    <div v-if="posts.length">
      <UBlogPosts :posts="posts" orientation="vertical" />
    </div>
    <div v-else>
      <UPageSection
        title="No Posts Yet"
        description="Create your first post for this blog"
        :links="[{ label: 'Create Post', icon: 'i-lucide-plus', color: 'primary', onClick: () => { if (blog) router.push(`/write?blogId=${blog.id}`) } }]"
      />
    </div>
  </template>
  <div v-else class="p-8 text-center">
    <p class="text-muted">Blog not found</p>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import type { BlogPostProps } from '@nuxt/ui'
import { useBlogStore } from '@/stores/blog-store'
import { useArticleStore } from '@/stores/article-store'
import type { PostResponse } from '@/stores/article-store'

const route = useRoute()
const router = useRouter()
const blogStore = useBlogStore()
const articleStore = useArticleStore()

const isLoading = ref(true)
const allPosts = ref<PostResponse[]>([])

const blog = computed(() => {
  const slug = route.params.slug as string
  return blogStore.blogs.find((b) => b.slug === slug) ?? null
})

const posts = computed<BlogPostProps[]>(() => {
  const drafts = allPosts.value
    .filter((p) => !p.isPublished)
    .sort((a, b) => {
      const dateA = a.createdOn ? new Date(a.createdOn).getTime() : 0
      const dateB = b.createdOn ? new Date(b.createdOn).getTime() : 0
      return dateB - dateA
    })

  const published = allPosts.value
    .filter((p) => p.isPublished)
    .sort((a, b) => {
      const dateA = a.publishedOn ? new Date(a.publishedOn).getTime() : 0
      const dateB = b.publishedOn ? new Date(b.publishedOn).getTime() : 0
      return dateB - dateA
    })

  const sorted = [...drafts, ...published]

  return sorted.map((post) => {
    const plainText = (post.content ?? '')
      .replace(/[#*`_~[\]]/g, '')
      .replace(/\n+/g, ' ')
      .trim()
    const description =
      plainText.length > 150 ? plainText.slice(0, 150) + '...' : plainText

    return {
      title: post.title,
      description,
      badge: post.isPublished ? 'Published' : 'Draft',
      to: `/write?blogId=${blog.value?.id}&edit=${post.id}`,
      coverImage: post.coverImageUrl ?? undefined,
      date: post.publishedOn ?? post.createdOn,
    } as BlogPostProps
  })
})

onMounted(async () => {
  await blogStore.update()
  if (blog.value?.id) {
    allPosts.value = await articleStore.fetchPostsByBlog(blog.value.id)
  }
  isLoading.value = false
})
</script>
