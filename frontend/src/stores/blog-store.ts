import { BackendApiSingleton } from '@/services/backend'
import type { components } from '@/services/backend/schema'
import { defineStore } from 'pinia'
import { computed, ref } from 'vue'

export const useBlogStore = defineStore('blogStore', () => {
  const blogs = ref<components['schemas']['BlogResponse'][]>([])

  async function update() {
    const client = await BackendApiSingleton.getInstance()
    const { data } = await client.GET('/blogs')
    blogs.value = data ?? []
  }

  async function createBlog(data: components['schemas']['CreateBlogCommand']) {
    const client = await BackendApiSingleton.getInstance()
    await client.POST('/blogs', { body: data })
  }

  async function updateBlog(id: string, data: components['schemas']['UpdateBlogCommand']) {
    const client = await BackendApiSingleton.getInstance()
    await client.PUT('/blogs/{id}', {
      params: { path: { id } },
      body: data,
    })
  }

  const projectBlogs = computed(() => blogs.value.filter((b) => b.kind === 'Project'))

  function blogBySlug(slug: string | undefined) {
    if (!slug) return null
    return blogs.value.find((b) => b.slug === slug) ?? null
  }

  return { blogs, projectBlogs, update, createBlog, updateBlog, blogBySlug }
})
