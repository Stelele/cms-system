<template>
  <div class="min-h-screen bg-gray-950 text-gray-100">
    <nav class="bg-gray-900 border-b border-gray-800">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex items-center justify-between h-16">
          <div class="flex items-center space-x-8">
            <RouterLink to="/" class="text-xl font-bold text-white">CMS</RouterLink>
            <div class="flex space-x-4">
              <RouterLink 
                to="/blogs" 
                class="px-3 py-2 rounded-md text-sm font-medium hover:bg-gray-800 transition"
                active-class="bg-gray-800 text-white"
              >
                Blogs
              </RouterLink>
            </div>
          </div>
          <div class="flex items-center">
            <span class="text-sm text-gray-400">{{ authStore.givenName }}</span>
          </div>
        </div>
      </div>
    </nav>

    <main class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <slot />
    </main>
  </div>
</template>

<script setup lang="ts">
import { useAuthStore } from "@/stores/AuthStore";

const authStore = useAuthStore();
</script>
