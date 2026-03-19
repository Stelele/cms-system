import { defineStore } from "pinia";
import { ref } from "vue";
import { useAuthStore } from "./AuthStore";

export interface Blog {
  id: string;
  name: string;
  slug: string;
  description: string;
  createdOn: string;
  updatedOn: string;
}

export const useBlogStore = defineStore("blogStore", () => {
  const blogs = ref<Blog[]>([]);
  const currentBlog = ref<Blog | null>(null);
  const isLoading = ref(false);
  const error = ref<string | null>(null);

  async function fetchBlogs() {
    isLoading.value = true;
    error.value = null;
    try {
      const authStore = useAuthStore();
      const api = await authStore.getApi();
      const response = await api.GET("/blogs");
      if (response.data) {
        blogs.value = response.data;
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : "Failed to fetch blogs";
    } finally {
      isLoading.value = false;
    }
  }

  async function fetchBlog(id: string) {
    isLoading.value = true;
    error.value = null;
    try {
      const authStore = useAuthStore();
      const api = await authStore.getApi();
      const response = await api.GET("/blogs/{id}", {
        params: { path: { id } },
      });
      if (response.data) {
        currentBlog.value = response.data;
        return response.data;
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : "Failed to fetch blog";
    } finally {
      isLoading.value = false;
    }
    return null;
  }

  async function createBlog(data: { name: string; slug: string; description: string }) {
    isLoading.value = true;
    error.value = null;
    try {
      const authStore = useAuthStore();
      const api = await authStore.getApi();
      const response = await api.POST("/blogs", {
        body: data,
      });
      if (response.data?.id) {
        await fetchBlogs();
        return response.data.id;
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : "Failed to create blog";
    } finally {
      isLoading.value = false;
    }
    return null;
  }

  async function updateBlog(id: string, data: { name: string; slug: string; description: string }) {
    isLoading.value = true;
    error.value = null;
    try {
      const authStore = useAuthStore();
      const api = await authStore.getApi();
      await api.PUT("/blogs/{id}", {
        params: { path: { id } },
        body: data,
      });
      await fetchBlogs();
      return true;
    } catch (e) {
      error.value = e instanceof Error ? e.message : "Failed to update blog";
    } finally {
      isLoading.value = false;
    }
    return false;
  }

  async function deleteBlog(id: string) {
    isLoading.value = true;
    error.value = null;
    try {
      const authStore = useAuthStore();
      const api = await authStore.getApi();
      await api.DELETE("/blogs/{id}", {
        params: { path: { id } },
      });
      await fetchBlogs();
      return true;
    } catch (e) {
      error.value = e instanceof Error ? e.message : "Failed to delete blog";
    } finally {
      isLoading.value = false;
    }
    return false;
  }

  return {
    blogs,
    currentBlog,
    isLoading,
    error,
    fetchBlogs,
    fetchBlog,
    createBlog,
    updateBlog,
    deleteBlog,
  };
});
