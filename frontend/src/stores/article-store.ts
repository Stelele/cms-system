import type { components } from '@/services/backend/schema'
import { BackendApiSingleton } from '@/services/backend'
import { defineStore } from 'pinia'
import { ref } from 'vue'

export type PostFormData = components['schemas']['CreatePostCommand']
export type PostUpdateData = components['schemas']['UpdatePostCommand']
export type PostResponse = components['schemas']['PostResponse']

export const useArticleStore = defineStore('articleStore', () => {
  const currentPost = ref<PostResponse | null>(null)
  const isLoading = ref(false)

  async function fetchPostsByBlog(blogId: string): Promise<PostResponse[]> {
    isLoading.value = true
    try {
      const client = await BackendApiSingleton.getInstance()
      const { data, error } = await client.GET('/blogs/{blogId}/posts', {
        params: { path: { blogId } },
      })

      if (error) {
        console.error('Failed to fetch posts:', error)
        return []
      }

      return data ?? []
    } finally {
      isLoading.value = false
    }
  }

  async function fetchPost(blogId: string, postId: string): Promise<PostResponse | null> {
    isLoading.value = true
    try {
      const client = await BackendApiSingleton.getInstance()
      const { data, error } = await client.GET('/blogs/{blogId}/posts/{id}', {
        params: { path: { blogId, id: postId } },
      })

      if (error) {
        console.error('Failed to fetch post:', error)
        return null
      }

      currentPost.value = data ?? null
      return data ?? null
    } finally {
      isLoading.value = false
    }
  }

  async function fetchPostBySlug(slug: string): Promise<PostResponse | null> {
    isLoading.value = true
    try {
      const client = await BackendApiSingleton.getInstance()
      const { data, error } = await client.GET('/posts/slug/{slug}', {
        params: { path: { slug } },
      })

      if (error) {
        console.error('Failed to fetch post by slug:', error)
        return null
      }

      currentPost.value = data ?? null
      return data ?? null
    } finally {
      isLoading.value = false
    }
  }

  async function createPost(blogId: string, postData: PostFormData): Promise<string | null> {
    isLoading.value = true
    try {
      const client = await BackendApiSingleton.getInstance()
      const { data, error } = await client.POST('/blogs/{blogId}/posts', {
        params: { path: { blogId } },
        body: postData,
      })

      if (error) {
        console.error('Failed to create post:', error)
        return null
      }

      return data?.id ?? null
    } finally {
      isLoading.value = false
    }
  }

  async function updatePost(
    blogId: string,
    postId: string,
    postData: PostUpdateData,
  ): Promise<boolean> {
    isLoading.value = true
    try {
      const client = await BackendApiSingleton.getInstance()
      const { error } = await client.PUT('/blogs/{blogId}/posts/{id}', {
        params: { path: { blogId, id: postId } },
        body: postData,
      })

      if (error) {
        console.error('Failed to update post:', error)
        return false
      }

      return true
    } finally {
      isLoading.value = false
    }
  }

  async function deletePost(blogId: string, postId: string): Promise<boolean> {
    isLoading.value = true
    try {
      const client = await BackendApiSingleton.getInstance()
      const { error } = await client.DELETE('/blogs/{blogId}/posts/{id}', {
        params: { path: { blogId, id: postId } },
      })

      if (error) {
        console.error('Failed to delete post:', error)
        return false
      }

      return true
    } finally {
      isLoading.value = false
    }
  }

  function clearCurrentPost() {
    currentPost.value = null
  }

  return {
    currentPost,
    isLoading,
    fetchPostsByBlog,
    fetchPost,
    fetchPostBySlug,
    createPost,
    updatePost,
    deletePost,
    clearCurrentPost,
  }
})
