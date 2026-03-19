<template>
  <DashboardLayout>
    <div class="max-w-4xl mx-auto space-y-6">
      <div class="flex items-center space-x-4">
        <UButton :to="`/blogs/${blogId}/posts`" variant="ghost" icon="i-heroicons-arrow-left" />
        <h1 class="text-3xl font-bold">{{ isEditing ? 'Edit Post' : 'New Post' }}</h1>
      </div>

      <form @submit.prevent="handleSubmit" class="bg-gray-900 rounded-lg border border-gray-800 p-6 space-y-6">
        <div class="grid grid-cols-2 gap-6">
          <UFormField label="Title" required class="col-span-2">
            <UInput 
              v-model="form.title" 
              placeholder="My Post Title"
              class="w-full"
            />
          </UFormField>

          <UFormField label="Slug" required>
            <UInput 
              v-model="form.slug" 
              placeholder="my-post-title"
              class="w-full"
            />
          </UFormField>

          <UFormField label="Tag" required>
            <UInput 
              v-model="form.tag" 
              placeholder="programming"
              class="w-full"
              list="tags"
            />
            <datalist id="tags">
              <option v-for="t in postStore.tags" :key="t.tag" :value="t.tag" />
            </datalist>
          </UFormField>

          <UFormField label="Cover Image URL" class="col-span-2">
            <UInput 
              v-model="form.coverImageUrl" 
              placeholder="https://example.com/image.jpg"
              class="w-full"
            />
          </UFormField>

          <UFormField label="Content (Markdown)" required class="col-span-2">
            <div class="grid grid-cols-2 gap-4">
              <UTextarea 
                v-model="form.content" 
                placeholder="# Hello World&#10;&#10;Write your content here..."
                class="w-full font-mono"
                rows="20"
              />
              <div class="border border-gray-700 rounded-lg p-4 overflow-auto bg-gray-950 max-h-96">
                <div v-html="renderedContent" class="prose prose-invert prose-sm max-w-none" />
              </div>
            </div>
          </UFormField>

          <UFormField label="Publish" class="col-span-2">
            <UCheckbox v-model="form.isPublished" label="Publish this post" />
          </UFormField>
        </div>

        <div class="flex items-center justify-between pt-4">
          <UButton 
            v-if="isEditing"
            type="button"
            variant="destructive"
            @click="handleDelete"
          >
            Delete Post
          </UButton>
          <div v-else />
          <UButton type="submit" color="primary" :loading="postStore.isLoading">
            {{ isEditing ? 'Save Changes' : 'Create Post' }}
          </UButton>
        </div>
      </form>
    </div>
  </DashboardLayout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import { useRoute, useRouter } from "vue-router";
import { marked } from "marked";
import { useBlogStore } from "@/stores/BlogStore";
import { usePostStore } from "@/stores/PostStore";
import DashboardLayout from "@/layouts/DashboardLayout.vue";

const route = useRoute();
const router = useRouter();
const blogStore = useBlogStore();
const postStore = usePostStore();

const blogId = computed(() => route.params.blogId as string);

const form = ref({
  title: "",
  slug: "",
  content: "",
  tag: "",
  coverImageUrl: "",
  isPublished: false,
});

const isEditing = computed(() => !!route.params.id);

const renderedContent = computed(() => {
  if (!form.value.content) return "";
  return marked(form.value.content);
});

onMounted(async () => {
  await blogStore.fetchBlog(blogId.value);
  await postStore.fetchTags();
  
  if (isEditing.value) {
    const post = await postStore.fetchPost(blogId.value, route.params.id as string);
    if (post) {
      form.value = {
        title: post.title,
        slug: post.slug,
        content: post.content,
        tag: post.tag,
        coverImageUrl: post.coverImageUrl || "",
        isPublished: post.isPublished,
      };
    }
  }
});

async function handleSubmit() {
  const data = {
    ...form.value,
    coverImageUrl: form.value.coverImageUrl || undefined,
  };

  if (isEditing.value) {
    const success = await postStore.updatePost(blogId.value, route.params.id as string, data);
    if (success) {
      router.push(`/blogs/${blogId.value}/posts`);
    }
  } else {
    const id = await postStore.createPost(blogId.value, data);
    if (id) {
      router.push(`/blogs/${blogId.value}/posts`);
    }
  }
}

async function handleDelete() {
  if (confirm("Are you sure you want to delete this post?")) {
    const success = await postStore.deletePost(blogId.value, route.params.id as string);
    if (success) {
      router.push(`/blogs/${blogId.value}/posts`);
    }
  }
}
</script>
