<template>
  <DashboardLayout>
    <div class="space-y-6">
      <div class="flex items-center justify-between">
        <div class="flex items-center space-x-4">
          <UButton to="/" variant="ghost" icon="i-heroicons-arrow-left" />
          <h1 class="text-3xl font-bold">Blogs</h1>
        </div>
        <UButton to="/blogs/new" color="primary">New Blog</UButton>
      </div>

      <div v-if="blogStore.isLoading" class="flex justify-center py-12">
        <UIcon name="i-heroicons-arrow-path" class="animate-spin w-8 h-8" />
      </div>

      <div v-else-if="blogStore.blogs.length === 0" class="text-center py-12 bg-gray-900 rounded-lg border border-gray-800">
        <p class="text-gray-400 text-lg">No blogs yet. Create your first blog to get started.</p>
        <UButton to="/blogs/new" color="primary" class="mt-4">Create Blog</UButton>
      </div>

      <UTable 
        v-else 
        :rows="blogStore.blogs" 
        :columns="columns"
        class="bg-gray-900 rounded-lg border border-gray-800"
      >
        <template #name-data="{ row }">
          <span class="font-medium text-white">{{ row.name }}</span>
        </template>
        <template #slug-data="{ row }">
          <span class="text-gray-400">/{{ row.slug }}</span>
        </template>
        <template #actions-data="{ row }">
          <div class="flex items-center space-x-2">
            <UButton 
              :to="`/blogs/${row.id}`" 
              variant="ghost" 
              size="sm"
            >
              Edit
            </UButton>
            <UButton 
              :to="`/blogs/${row.id}/posts`" 
              variant="ghost" 
              size="sm"
            >
              Posts
            </UButton>
          </div>
        </template>
      </UTable>
    </div>
  </DashboardLayout>
</template>

<script setup lang="ts">
import { onMounted } from "vue";
import { useBlogStore } from "@/stores/BlogStore";
import DashboardLayout from "@/layouts/DashboardLayout.vue";

const blogStore = useBlogStore();

const columns = [
  { key: "name", label: "Name" },
  { key: "slug", label: "Slug" },
  { key: "description", label: "Description" },
  { key: "actions", label: "Actions" },
];

onMounted(() => {
  blogStore.fetchBlogs();
});
</script>
