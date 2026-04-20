<template>
  <div class="space-y-2">
    <div class="flex flex-wrap gap-2">
      <UButton
        v-for="icon in quickIcons"
        :key="icon"
        :icon="icon"
        :variant="modelValue === icon ? 'solid' : 'outline'"
        :color="modelValue === icon ? 'primary' : 'gray'"
        size="sm"
        @click="$emit('update:modelValue', icon)"
      />
    </div>
    <UInput
      :model-value="modelValue"
      placeholder="i-heroicons-book-open"
      @update:model-value="$emit('update:modelValue', $event)"
    >
      <template #leading>
        <UIcon :name="modelValue" class="text-lg" />
      </template>
    </UInput>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  modelValue: string
}>()

defineEmits<{
  'update:modelValue': [value: string]
}>()

const quickIcons = [
  'i-heroicons-book-open',
  'i-heroicons-document-text',
  'i-heroicons-chat-bubble-left',
  'i-heroicons-photo',
  'i-heroicons-video-camera',
  'i-heroicons-music-note',
  'i-heroicons-code-bracket',
  'i-lucide-zap',
  'i-mdi-language-typescript',
  'i-mdi-language-javascript',
  'i-mdi-language-python',
  'i-mdi-language-cpp',
]
</script>