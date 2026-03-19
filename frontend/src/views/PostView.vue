<template>
  <div class="min-h-screen bg-gray-950 text-gray-100">
    <nav class="bg-gray-900 border-b border-gray-800">
      <div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex items-center justify-between h-16">
          <RouterLink to="/" class="text-xl font-bold text-white">CMS</RouterLink>
          <UButton to="/blogs" variant="ghost" size="sm">Dashboard</UButton>
        </div>
      </div>
    </nav>

    <main class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <div v-if="postStore.isLoading" class="flex justify-center py-12">
        <UIcon name="i-heroicons-arrow-path" class="animate-spin w-8 h-8" />
      </div>

      <article v-else-if="post" class="space-y-6">
        <img 
          v-if="post.coverImageUrl" 
          :src="post.coverImageUrl" 
          :alt="post.title"
          class="w-full h-64 object-cover rounded-lg"
        />
        
        <div class="flex items-center space-x-3">
          <UBadge color="primary" variant="subtle">{{ post.tag }}</UBadge>
          <span class="text-gray-400 text-sm">
            {{ formatDate(post.publishedOn || post.updatedOn) }}
          </span>
        </div>

        <h1 class="text-4xl font-bold text-white">{{ post.title }}</h1>

        <div class="prose prose-invert prose-lg max-w-none" v-html="renderedContent" />
      </article>

      <div v-else class="text-center py-12">
        <h1 class="text-2xl font-bold text-white">Post not found</h1>
        <p class="text-gray-400 mt-2">The post you're looking for doesn't exist.</p>
        <UButton to="/" variant="ghost" class="mt-4">Go Home</UButton>
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from "vue";
import { useRoute } from "vue-router";
import { marked } from "marked";
import { usePostStore } from "@/stores/PostStore";

const route = useRoute();
const postStore = usePostStore();

const post = computed(() => postStore.currentPost);

const renderedContent = computed(() => {
  if (!post.value?.content) return "";
  return marked(post.value.content);
});

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString("en-US", {
    year: "numeric",
    month: "long",
    day: "numeric",
  });
}

onMounted(async () => {
  const slug = route.params.slug as string;
  await postStore.fetchPostBySlug(slug);
});
</script>
