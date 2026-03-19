<template>
  <DashboardLayout>
    <div class="space-y-6">
      <div class="flex items-center justify-between">
        <h1 class="text-3xl font-bold">Dashboard</h1>
        <UButton to="/blogs/new" color="primary">Create Blog</UButton>
      </div>

      <div v-if="blogStore.isLoading" class="flex justify-center py-12">
        <UIcon name="i-heroicons-arrow-path" class="animate-spin w-8 h-8" />
      </div>

      <div v-else-if="blogStore.blogs.length === 0" class="text-center py-12">
        <p class="text-gray-400 text-lg">No blogs yet. Create your first blog to get started.</p>
        <UButton to="/blogs/new" color="primary" class="mt-4">Create Blog</UButton>
      </div>

      <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        <div 
          v-for="blog in blogStore.blogs" 
          :key="blog.id"
          class="bg-gray-900 rounded-lg border border-gray-800 p-6 hover:border-gray-700 transition"
        >
          <div class="flex items-start justify-between">
            <div>
              <h3 class="text-xl font-semibold text-white">{{ blog.name }}</h3>
              <p class="text-sm text-gray-400 mt-1">/{{ blog.slug }}</p>
            </div>
            <UBadge color="primary" variant="subtle">{{ blog.postCount || 0 }} posts</UBadge>
          </div>
          <p class="text-gray-300 mt-3">{{ blog.description }}</p>
          <div class="flex items-center justify-end mt-4 space-x-2">
            <UButton 
              :to="`/blogs/${blog.id}`" 
              variant="ghost" 
              size="sm"
            >
              Edit
            </UButton>
            <UButton 
              :to="`/blogs/${blog.id}/posts`" 
              variant="ghost" 
              size="sm"
            >
              View Posts
            </UButton>
          </div>
        </div>
      </div>
    </div>
  </DashboardLayout>
</template>

<script setup lang="ts">
import { onMounted } from "vue";
import { useBlogStore } from "@/stores/BlogStore";
import DashboardLayout from "@/layouts/DashboardLayout.vue";

const blogStore = useBlogStore();

onMounted(() => {
  blogStore.fetchBlogs();
});
</script>
