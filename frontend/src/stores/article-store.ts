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
  const publishingPostIds = ref(new Set<string>())

  async function fetchPostsByBlog(
    blogId: string,
    options?: { isPublished?: boolean; tag?: string },
  ): Promise<PostResponse[]> {
    isLoading.value = true
    try {
      const client = await BackendApiSingleton.getInstance()
      const { data, error } = await client.GET('/blogs/{blogId}/posts', {
        params: { path: { blogId }, query: { isPublished: options?.isPublished, tag: options?.tag } },
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

  async function fetchDraftsByBlog(blogId: string): Promise<PostResponse[]> {
    return fetchPostsByBlog(blogId, { isPublished: false })
  }

  async function fetchAllDrafts(): Promise<Map<string, PostResponse[]>> {
    const { useBlogStore } = await import('@/stores/blog-store')
    const blogStore = useBlogStore()
    await blogStore.update()

    const draftsByBlog = new Map<string, PostResponse[]>()

    const results = await Promise.all(
      blogStore.blogs
        .filter((blog) => blog.id)
        .map(async (blog) => {
          const posts = await fetchDraftsByBlog(blog.id!)
          return { blogId: blog.id!, posts }
        }),
    )

    for (const { blogId, posts } of results) {
      if (posts.length > 0) {
        draftsByBlog.set(blogId, posts)
      }
    }

    return draftsByBlog
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

      return data ?? null
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

  async function quickPublish(post: PostResponse): Promise<boolean> {
    publishingPostIds.value.add(post.id!)
    try {
      const postData: PostUpdateData = {
        blogId: post.blogId!,
        id: post.id!,
        title: post.title ?? '',
        slug: post.slug ?? '',
        content: post.content ?? '',
        tag: post.tag ?? '',
        coverImageUrl: post.coverImageUrl ?? null,
        isPublished: true,
      }
      return await updatePost(post.blogId!, post.id!, postData)
    } finally {
      publishingPostIds.value.delete(post.id!)
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

  function isPublishing(postId: string): boolean {
    return publishingPostIds.value.has(postId)
  }

  return {
    currentPost,
    isLoading,
    publishingPostIds,
    fetchPostsByBlog,
    fetchDraftsByBlog,
    fetchAllDrafts,
    fetchPost,
    fetchPostBySlug,
    createPost,
    updatePost,
    quickPublish,
    deletePost,
    clearCurrentPost,
    isPublishing,
  }
})
