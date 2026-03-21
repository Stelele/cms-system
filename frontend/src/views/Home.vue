<template>
  <UPageHero title="The Sandbox" description="Build, break, write, and reflect." :links="links" />
</template>

<script setup lang="ts">
import { ref } from 'vue'
import type { ButtonProps } from '@nuxt/ui'
import { useNavStore } from '@/stores/nav-store'

const { navigateTo } = useNavStore()
const links = ref<ButtonProps[]>([
  {
    label: 'Write Post',
    icon: 'i-lucide-pencil',
    trailingIcon: 'i-lucide-arrow-right',
    onClick: () => navigateTo('/write'),
  },
  {
    label: 'Draft Posts',
    color: 'neutral',
    variant: 'subtle',
    icon: 'i-lucide-files',
    onClick: () => navigateTo('/drafts'),
  },
])
</script>
