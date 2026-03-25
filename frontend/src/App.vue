<template>
  <UApp>
    <UHeader :mode="'drawer'" title="">
      <template #body>
        <UNavigationMenu :items="navStore.items" orientation="vertical" class="-mx-2.5" />
      </template>
    </UHeader>
    <UMain>
      <PageBase>
        <RouterView />
      </PageBase>
    </UMain>
  </UApp>
</template>

<script setup lang="ts">
import PageBase from '@/layouts/PageBase.vue'
import { useNavStore } from '@/stores/nav-store'
import { onBeforeMount } from 'vue'
import { useAuthStore } from './stores/auth-store'
import { useBlogStore } from './stores/blog-store'

const navStore = useNavStore()
const authStore = useAuthStore()
const blogStore = useBlogStore()

onBeforeMount(async () => {
  await authStore.update()
  blogStore.update()
})
</script>
