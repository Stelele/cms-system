import { BackendApiSingleton } from '@/services/backend'
import type { components } from '@/services/backend/schema'
import { defineStore } from 'pinia'
import { ref } from 'vue'

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

  return { blogs, update, createBlog }
})
