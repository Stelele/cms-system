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

const navStore = useNavStore()
</script>
