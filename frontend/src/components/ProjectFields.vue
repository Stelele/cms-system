<template>
  <div class="space-y-4">
    <UFormField
      label="Category"
      name="category"
      :required="true"
      :description="categoryDescription"
    >
      <USelect
        v-model="local.category"
        :items="categoryItems"
        value-key="value"
        class="w-full"
        :disabled="lockedCategory"
      />
    </UFormField>

    <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
      <UFormField
        label="Year"
        name="year"
        :required="true"
        description="The year you finished, or started if unfinished."
      >
        <UInput v-model="yearInput" type="number" min="2000" max="2100" class="w-full" />
      </UFormField>

      <UFormField
        label="Last pushed"
        name="lastPushedAt"
        description="Drives the Active/Archived status. Optional."
      >
        <UInput v-model="lastPushedInput" type="date" class="w-full" />
      </UFormField>
    </div>

    <UFormField
      label="Stack"
      name="stack"
      description="Languages, frameworks and tools. Press Enter to add one."
    >
      <UInput
        v-model="stackInput"
        placeholder="Vue, WebGPU, Go"
        class="w-full"
        @keydown.enter.prevent="addStackItem"
      />
      <div v-if="local.stack.length" class="mt-2 flex flex-wrap gap-2">
        <UBadge
          v-for="(item, index) in local.stack"
          :key="`${item}-${index}`"
          color="neutral"
          variant="subtle"
          size="sm"
        >
          <span class="flex items-center gap-1">
            {{ item }}
            <UButton
              icon="i-lucide-x"
              color="neutral"
              variant="link"
              size="xs"
              :aria-label="`Remove ${item}`"
              @click="removeStackItem(index)"
            />
          </span>
        </UBadge>
      </div>
    </UFormField>

    <UFormField
      label="Links"
      name="links"
      description="Repository, live site, write-up. Rows without a label or URL are ignored on save."
    >
      <div class="w-full space-y-2">
        <div
          v-for="(link, index) in local.links"
          :key="index"
          class="flex w-full items-start gap-2"
        >
          <UInput
            v-model="link.label"
            placeholder="Repository"
            class="w-40"
            :aria-label="`Link ${index + 1} label`"
          />
          <UInput
            v-model="link.url"
            placeholder="https://github.com/…"
            class="flex-1"
            :aria-label="`Link ${index + 1} URL`"
          />
          <UButton
            icon="i-lucide-trash-2"
            color="error"
            variant="ghost"
            :aria-label="`Remove link ${index + 1}`"
            @click="removeLink(index)"
          />
        </div>
        <UButton
          icon="i-lucide-plus"
          color="neutral"
          variant="outline"
          size="sm"
          label="Add link"
          @click="addLink"
        />
      </div>
    </UFormField>

    <UAlert
      v-if="status"
      color="neutral"
      variant="subtle"
      icon="i-lucide-info"
      title="Status is derived, not set"
      :description="statusDescription"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import type { components } from '@/services/backend/schema'
import { PROJECT_BLOGS, type ProjectFormData } from '@/stores/project-store'

type ProjectCategory = components['schemas']['ProjectCategory']

const props = defineProps<{
  modelValue: ProjectFormData
  /** The blog's category wins: the API refuses a project whose category disagrees with its blog. */
  blogCategory: ProjectCategory | null
  blogSlug?: string
}>()

const emit = defineEmits<{
  'update:modelValue': [value: ProjectFormData]
}>()

const stackInput = ref('')

// Declared before the proxies below, which close over it.
const local = reactive<ProjectFormData>({ ...props.modelValue, links: [...props.modelValue.links] })

// UInput models are string-only, while the API types allow number | string and
// null. These proxies keep the store types honest without loosening them.
const yearInput = computed({
  get: () => (local.year === null || local.year === undefined ? '' : String(local.year)),
  set: (value: string) => {
    local.year = value === '' ? '' : value
  },
})

const lastPushedInput = computed({
  get: () => local.lastPushedAt ?? '',
  set: (value: string) => {
    local.lastPushedAt = value === '' ? null : value
  },
})

// Equal by value, including links. Without this the component loops: it emits,
// the parent replaces the ref, the props watcher writes a fresh `links` array
// into `local`, the deep watch fires and emits again, forever. That showed up as
// "Maximum recursive updates exceeded in <Write>" and, worse, meant values
// loaded from the CMS were overwritten before they could stick.
function sameAsLocal(value: ProjectFormData): boolean {
  return (
    value.category === local.category &&
    String(value.year) === String(local.year) &&
    value.lastPushedAt === local.lastPushedAt &&
    value.status === local.status &&
    value.stack.length === local.stack.length &&
    value.stack.every((item, i) => item === local.stack[i]) &&
    value.links.length === local.links.length &&
    value.links.every((l, i) => l.label === local.links[i]?.label && l.url === local.links[i]?.url)
  )
}

watch(
  () => props.modelValue,
  (value) => {
    if (sameAsLocal(value)) return
    Object.assign(local, value)
    local.links = value.links.map((l) => ({ ...l }))
  },
  { deep: true },
)

watch(
  local,
  () => {
    emit('update:modelValue', {
      category: local.category,
      year: local.year,
      stack: [...local.stack],
      lastPushedAt: local.lastPushedAt,
      links: local.links.map((l) => ({ ...l })),
      status: local.status,
    })
  },
  { deep: true },
)

const categoryItems = PROJECT_BLOGS.map((b) => ({ label: b.label, value: b.category }))

const lockedCategory = computed(() => props.blogCategory !== null)

watch(
  () => props.blogCategory,
  (category) => {
    if (category) local.category = category
  },
  { immediate: true },
)

const categoryDescription = computed(() =>
  props.blogCategory
    ? `Fixed to the "${props.blogSlug}" blog. The API rejects a project whose category disagrees with its blog, so this is not editable here.`
    : 'Choose a category. It must match the blog this project is saved into.',
)

const status = computed(() => local.status)
const statusDescription = computed(() =>
  local.status === 'Active'
    ? 'Active: pushed within the last year. Archived: older than that. It updates as you change the date above.'
    : 'Archived: not pushed for over a year.',
)

function addStackItem() {
  const value = stackInput.value.trim()
  if (!value) return
  if (!local.stack.includes(value)) local.stack.push(value)
  stackInput.value = ''
}

function removeStackItem(index: number) {
  local.stack.splice(index, 1)
}

function addLink() {
  local.links.push({ label: '', url: '' })
}

function removeLink(index: number) {
  local.links.splice(index, 1)
}
</script>
