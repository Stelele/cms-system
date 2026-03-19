import { defineStore } from "pinia";
import { ref } from "vue";
import { useAuthStore } from "./AuthStore";

export interface Post {
  id: string;
  blogId: string;
  title: string;
  slug: string;
  content: string;
  tag: string;
  coverImageUrl: string | null;
  publishedOn: string | null;
  isPublished: boolean;
  createdOn: string;
  updatedOn: string;
}

export interface Tag {
  tag: string;
  count: number;
}

export const usePostStore = defineStore("postStore", () => {
  const posts = ref<Post[]>([]);
  const currentPost = ref<Post | null>(null);
  const tags = ref<Tag[]>([]);
  const isLoading = ref(false);
  const error = ref<string | null>(null);

  async function fetchPostsByBlog(blogId: string, options?: { tag?: string; isPublished?: boolean }) {
    isLoading.value = true;
    error.value = null;
    try {
      const authStore = useAuthStore();
      const api = await authStore.getApi();
      const response = await api.GET("/blogs/{blogId}/posts", {
        params: {
          path: { blogId },
          query: {
            tag: options?.tag,
            isPublished: options?.isPublished,
          },
        },
      });
      if (response.data) {
        posts.value = response.data;
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : "Failed to fetch posts";
    } finally {
      isLoading.value = false;
    }
  }

  async function fetchPost(blogId: string, id: string) {
    isLoading.value = true;
    error.value = null;
    try {
      const authStore = useAuthStore();
      const api = await authStore.getApi();
      const response = await api.GET("/blogs/{blogId}/posts/{id}", {
        params: { path: { blogId, id } },
      });
      if (response.data) {
        currentPost.value = response.data;
        return response.data;
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : "Failed to fetch post";
    } finally {
      isLoading.value = false;
    }
    return null;
  }

  async function fetchPostBySlug(slug: string) {
    isLoading.value = true;
    error.value = null;
    try {
      const authStore = useAuthStore();
      const api = await authStore.getApi();
      const response = await api.GET("/posts/slug/{slug}", {
        params: { path: { slug } },
      });
      if (response.data) {
        currentPost.value = response.data;
        return response.data;
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : "Failed to fetch post";
    } finally {
      isLoading.value = false;
    }
    return null;
  }

  async function fetchTags() {
    isLoading.value = true;
    error.value = null;
    try {
      const authStore = useAuthStore();
      const api = await authStore.getApi();
      const response = await api.GET("/tags");
      if (response.data) {
        tags.value = response.data;
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : "Failed to fetch tags";
    } finally {
      isLoading.value = false;
    }
  }

  async function createPost(blogId: string, data: {
    title: string;
    slug: string;
    content: string;
    tag: string;
    coverImageUrl?: string;
    isPublished: boolean;
  }) {
    isLoading.value = true;
    error.value = null;
    try {
      const authStore = useAuthStore();
      const api = await authStore.getApi();
      const response = await api.POST("/blogs/{blogId}/posts", {
        params: { path: { blogId } },
        body: data,
      });
      if (response.data?.id) {
        await fetchPostsByBlog(blogId);
        return response.data.id;
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : "Failed to create post";
    } finally {
      isLoading.value = false;
    }
    return null;
  }

  async function updatePost(blogId: string, id: string, data: {
    title: string;
    slug: string;
    content: string;
    tag: string;
    coverImageUrl?: string;
    isPublished: boolean;
  }) {
    isLoading.value = true;
    error.value = null;
    try {
      const authStore = useAuthStore();
      const api = await authStore.getApi();
      await api.PUT("/blogs/{blogId}/posts/{id}", {
        params: { path: { blogId, id } },
        body: data,
      });
      await fetchPostsByBlog(blogId);
      return true;
    } catch (e) {
      error.value = e instanceof Error ? e.message : "Failed to update post";
    } finally {
      isLoading.value = false;
    }
    return false;
  }

  async function deletePost(blogId: string, id: string) {
    isLoading.value = true;
    error.value = null;
    try {
      const authStore = useAuthStore();
      const api = await authStore.getApi();
      await api.DELETE("/blogs/{blogId}/posts/{id}", {
        params: { path: { blogId, id } },
      });
      await fetchPostsByBlog(blogId);
      return true;
    } catch (e) {
      error.value = e instanceof Error ? e.message : "Failed to delete post";
    } finally {
      isLoading.value = false;
    }
    return false;
  }

  return {
    posts,
    currentPost,
    tags,
    isLoading,
    error,
    fetchPostsByBlog,
    fetchPost,
    fetchPostBySlug,
    fetchTags,
    createPost,
    updatePost,
    deletePost,
  };
});
