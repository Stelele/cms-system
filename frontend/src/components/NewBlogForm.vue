<template>
  <div class="w-full h-full grid grid-cols-1 gap-4 p-4">
    <UForm :schema="schema" :state="state" class="space-y-4">
      <UFormField
        label="Name"
        name="name"
        :required="true"
        description="Please enter the name of the blog."
      >
        <UInput v-model="state.name" class="w-full" />
      </UFormField>

      <UFormField
        label="Slug"
        name="slug"
        :required="true"
        description="Please enter the slug of the blog."
      >
        <UInput v-model="state.slug" class="w-full" />
      </UFormField>

      <UFormField
        label="Description"
        name="description"
        :required="true"
        description="Please enter a description of the expense."
      >
        <UTextarea v-model="state.description" class="w-full" :rows="6" />
      </UFormField>

      <div class="w-full flex justify-end">
        <UButton type="button" class="hover:cursor-pointer" @click="onButtonClick"> Submit </UButton>
      </div>
    </UForm>
  </div>
</template>

<script setup lang="ts">
import * as z from 'zod'
import { computed, reactive } from 'vue'
import type { components } from '@/services/backend/schema'
import { useBlogStore } from '@/stores/blog-store'
import { BackendApiSingleton } from '@/services/backend'

const blogStore = useBlogStore()

const blogNames = computed(() => blogStore.blogs.map((blog) => blog.name))
const blogSlugs = computed(() => blogStore.blogs.map((blog) => blog.slug))

type CreateBlogCommand = components['schemas']['CreateBlogCommand']

const schema = z.object({
  name: z
    .string()
    .min(4)
    .refine((value) => !blogNames.value.includes(value), {
      message: 'Blog name already exists',
    }),
  slug: z
    .string()
    .min(4)
    .refine((value) => !blogSlugs.value.includes(value), {
      message: 'Blog slug already exists',
    }),
  description: z.string(),
})
type Schema = z.output<typeof schema>

const state = reactive<Schema>({
  name: '',
  slug: '',
  description: '',
})

async function onButtonClick() {
  const expense: CreateBlogCommand = {
    name: state.name,
    slug: state.slug,
    description: state.description,
  }

  const client = await BackendApiSingleton.getInstance()
  const result = await client.POST('/blogs', { body: expense })

  if (result.data) {
    await blogStore.update()
  }
}
</script>
