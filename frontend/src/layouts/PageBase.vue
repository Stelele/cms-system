<template>
  <UPage>
    <template #left>
      <UPageAside>
        <UNavigationMenu
          orientation="vertical"
          :items="navStore.items"
          class="data-[orientation=vertical]:w-48"
        />
      </UPageAside>
    </template>
    <UPageBody class="h-[80vh] max-h-[80vh] overflow-y-auto">
      <slot />
    </UPageBody>
  </UPage>
</template>

<script lang="ts" setup>
import { ref } from 'vue'
import type { NavigationMenuItem } from '@nuxt/ui'
import { useNavStore } from '@/stores/nav-store'

const navStore = useNavStore()
</script>
