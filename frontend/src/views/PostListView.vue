<template>
  <DashboardLayout>
    <div class="space-y-6">
      <div class="flex items-center justify-between">
        <div class="flex items-center space-x-4">
          <UButton to="/" variant="ghost" icon="i-heroicons-arrow-left" />
          <div>
            <h1 class="text-3xl font-bold">{{ blogStore.currentBlog?.name || 'Posts' }}</h1>
            <p class="text-gray-400 text-sm">/{{ blogStore.currentBlog?.slug }}</p>
          </div>
        </div>
        <UButton :to="`/blogs/${blogId}/posts/new`" color="primary">New Post</UButton>
      </div>

      <div class="flex items-center space-x-4">
        <USelect
          v-model="selectedTag"
          :items="tagOptions"
          placeholder="Filter by tag"
          class="w-48"
        />
        <USelect
          v-model="selectedStatus"
          :items="statusOptions"
          placeholder="Filter by status"
          class="w-48"
        />
      </div>

      <div v-if="postStore.isLoading" class="flex justify-center py-12">
        <UIcon name="i-heroicons-arrow-path" class="animate-spin w-8 h-8" />
      </div>

      <div v-else-if="postStore.posts.length === 0" class="text-center py-12 bg-gray-900 rounded-lg border border-gray-800">
        <p class="text-gray-400 text-lg">No posts yet. Create your first post to get started.</p>
        <UButton :to="`/blogs/${blogId}/posts/new`" color="primary" class="mt-4">Create Post</UButton>
      </div>

      <div v-else class="space-y-4">
        <div 
          v-for="post in postStore.posts" 
          :key="post.id"
          class="bg-gray-900 rounded-lg border border-gray-800 p-6 hover:border-gray-700 transition"
        >
          <div class="flex items-start justify-between">
            <div class="flex-1">
              <div class="flex items-center space-x-3">
                <h3 class="text-xl font-semibold text-white">{{ post.title }}</h3>
                <UBadge :color="post.isPublished ? 'green' : 'gray'" variant="subtle">
                  {{ post.isPublished ? 'Published' : 'Draft' }}
                </UBadge>
                <UBadge color="primary" variant="subtle">{{ post.tag }}</UBadge>
              </div>
              <p class="text-gray-400 text-sm mt-1">/{{ post.slug }}</p>
            </div>
          </div>
          <div class="flex items-center justify-end mt-4 space-x-2">
            <UButton 
              :to="`/blogs/${blogId}/posts/${post.id}/edit`" 
              variant="ghost" 
              size="sm"
            >
              Edit
            </UButton>
            <UButton 
              :to="`/p/${post.slug}`" 
              variant="ghost" 
              size="sm"
              external
            >
              View
            </UButton>
          </div>
        </div>
      </div>
    </div>
  </DashboardLayout>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from "vue";
import { useRoute } from "vue-router";
import { useBlogStore } from "@/stores/BlogStore";
import { usePostStore } from "@/stores/PostStore";
import DashboardLayout from "@/layouts/DashboardLayout.vue";

const route = useRoute();
const blogStore = useBlogStore();
const postStore = usePostStore();

const blogId = computed(() => route.params.blogId as string);
const selectedTag = ref<string | null>(null);
const selectedStatus = ref<boolean | null>(null);

const tagOptions = computed(() => [
  { label: "All Tags", value: "" },
  ...postStore.tags.map(t => ({ label: `${t.tag} (${t.count})`, value: t.tag })),
]);

const statusOptions = [
  { label: "All", value: "" },
  { label: "Published", value: "true" },
  { label: "Draft", value: "false" },
];

async function fetchData() {
  await blogStore.fetchBlog(blogId.value);
  await postStore.fetchPostsByBlog(blogId.value, {
    tag: selectedTag.value || undefined,
    isPublished: selectedStatus.value === "true" ? true : selectedStatus.value === "false" ? false : undefined,
  });
  await postStore.fetchTags();
}

onMounted(fetchData);

watch([selectedTag, selectedStatus], fetchData);
</script>
