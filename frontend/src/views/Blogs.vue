<template>
  <UPageHeader title="Blogs" :links class="pe-4" />
  <div v-if="posts.length">
    <UBlogPosts :posts="posts" />
  </div>
  <div v-else>
    <UPageSection
      title="Create Your First Blog"
      description="We must start somewhere"
      :links="links"
    />
  </div>
  <UModal title="Create Blog" description="For adding a new blog" v-model:open="openNewBlogModal">
    <template #content>
      <NewBlogForm />
    </template>
  </UModal>
</template>

<script setup lang="ts">
import type { BlogPostProps, ButtonProps } from '@nuxt/ui'
import { computed, ref } from 'vue'
import { useBlogStore } from '@/stores/blog-store'

const blogStore = useBlogStore()
const posts = computed<BlogPostProps[]>(() =>
  blogStore.blogs.map(
    (blog) =>
      ({
        title: blog.name,
        description: blog.description,
        to: `/blog/${blog.slug}`,
      }) as BlogPostProps,
  ),
)

const openNewBlogModal = ref(false)
const links = ref<ButtonProps[]>([
  {
    label: 'Create Blog',
    icon: 'i-lucide-plus',
    color: 'primary',
    onClick: () => {
      openNewBlogModal.value = true
    },
  },
])
</script>
