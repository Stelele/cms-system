<template>
  <UBlogPost
    :title="post.title ?? ''"
    :description="displayDescription"
    :image="post.coverImageUrl ?? undefined"
    :date="post[dateKey] ?? undefined"
    :badge="status === 'draft' ? { label: 'Draft', color: 'neutral', variant: 'subtle' } : { label: 'Published', color: 'success', variant: 'subtle' }"
    :to="editLink"
    orientation="vertical"
    class="group"
  >
    <template #footer>
      <div v-if="status === 'draft'" class="flex gap-2">
        <UButton
          size="sm"
          color="primary"
          :loading="isPublishing"
          :disabled="isPublishing"
          @click="$emit('publish', post)"
        >
          Publish
        </UButton>
        <UButton size="sm" color="neutral" variant="outline" :to="editLink">
          Edit
        </UButton>
      </div>
      <UButton v-else size="sm" color="neutral" variant="outline" :to="editLink">
        Edit
      </UButton>
    </template>
  </UBlogPost>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { PostResponse } from '@/stores/article-store'
import { stripMarkdown } from '@/utils/markdown'

const props = defineProps<{
  post: PostResponse
  blogId: string | undefined
  status: 'draft' | 'published'
  isPublishing?: boolean
}>()

defineEmits<{
  publish: [post: PostResponse]
}>()

const editLink = computed(() => `/write?blogId=${props.blogId ?? ''}&edit=${props.post.id}`)

const dateKey = computed(() => props.status === 'draft' ? 'createdOn' : 'publishedOn')

const strippedContent = computed(() => stripMarkdown(props.post.content ?? ''))

const displayDescription = computed(() => props.post.description ?? strippedContent.value)
</script>
