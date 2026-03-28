<template>
  <div class="px-4 py-16 text-center">
    <div class="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-(--ui-bg-elevated)">
      <UIcon :name="icon" class="h-8 w-8 text-muted" />
    </div>
    <h2 class="mb-2 text-xl font-semibold">{{ title }}</h2>
    <p class="text-muted">{{ description }}</p>
    <UButton v-if="actionLabel" class="mt-6" :color="actionColor ?? 'primary'" :to="actionTo" @click="$emit('action')">
      {{ actionLabel }}
    </UButton>
  </div>
</template>

<script setup lang="ts">
withDefaults(
  defineProps<{
    icon?: string
    title: string
    description: string
    actionLabel?: string
    actionTo?: string
    actionColor?: 'primary' | 'neutral' | 'success' | 'warning' | 'error' | 'info'
  }>(),
  {
    icon: 'i-lucide-file-text',
  },
)

defineEmits<{
  action: []
}>()
</script>
