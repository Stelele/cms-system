<template>
  <DashboardLayout>
    <div class="max-w-2xl mx-auto space-y-6">
      <div class="flex items-center space-x-4">
        <UButton to="/blogs" variant="ghost" icon="i-heroicons-arrow-left" />
        <h1 class="text-3xl font-bold">{{ isEditing ? 'Edit Blog' : 'New Blog' }}</h1>
      </div>

      <form @submit.prevent="handleSubmit" class="bg-gray-900 rounded-lg border border-gray-800 p-6 space-y-6">
        <UFormField label="Name" required>
          <UInput 
            v-model="form.name" 
            placeholder="My Blog"
            class="w-full"
          />
        </UFormField>

        <UFormField label="Slug" required>
          <UInput 
            v-model="form.slug" 
            placeholder="my-blog"
            class="w-full"
          />
          <p class="text-sm text-gray-400 mt-1">Used in URLs: /blogs/{{ form.slug || 'slug' }}</p>
        </UFormField>

        <UFormField label="Description" required>
          <UTextarea 
            v-model="form.description" 
            placeholder="A brief description of your blog..."
            class="w-full"
            rows="3"
          />
        </UFormField>

        <div class="flex items-center justify-between pt-4">
          <UButton 
            v-if="isEditing"
            type="button"
            variant="destructive"
            @click="handleDelete"
          >
            Delete Blog
          </UButton>
          <div v-else />
          <UButton type="submit" color="primary" :loading="blogStore.isLoading">
            {{ isEditing ? 'Save Changes' : 'Create Blog' }}
          </UButton>
        </div>
      </form>
    </div>
  </DashboardLayout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useBlogStore } from "@/stores/BlogStore";
import DashboardLayout from "@/layouts/DashboardLayout.vue";

const route = useRoute();
const router = useRouter();
const blogStore = useBlogStore();

const form = ref({
  name: "",
  slug: "",
  description: "",
});

const isEditing = computed(() => !!route.params.id);

onMounted(async () => {
  if (isEditing.value) {
    const blog = await blogStore.fetchBlog(route.params.id as string);
    if (blog) {
      form.value = {
        name: blog.name,
        slug: blog.slug,
        description: blog.description,
      };
    }
  }
});

async function handleSubmit() {
  if (isEditing.value) {
    const success = await blogStore.updateBlog(route.params.id as string, form.value);
    if (success) {
      router.push("/blogs");
    }
  } else {
    const id = await blogStore.createBlog(form.value);
    if (id) {
      router.push("/blogs");
    }
  }
}

async function handleDelete() {
  if (confirm("Are you sure you want to delete this blog? All posts will be deleted.")) {
    const success = await blogStore.deleteBlog(route.params.id as string);
    if (success) {
      router.push("/blogs");
    }
  }
}
</script>
