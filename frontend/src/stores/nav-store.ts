import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import type { NavigationMenuItem } from '@nuxt/ui'
import { useRouter } from 'vue-router'

export const useNavStore = defineStore('navStore', () => {
  const router = useRouter()
  const currentLink = ref<NavigationMenuItem | undefined>()
  const items = computed<NavigationMenuItem[]>(() => [
    {
      label: 'Home',
      icon: 'i-lucide-home',
      to: '/',
    },
    {
      label: 'Blogs',
      icon: 'i-lucide-folder',
      to: '/blogs',
    },
    {
      label: 'Write',
      icon: 'i-lucide-pencil',
      to: '/write',
    },
    {
      label: 'Drafts',
      icon: 'i-lucide-files',
      to: '/drafts',
    },
  ])

  function init(path: string) {
    const matches: Array<[number, NavigationMenuItem]> = []
    for (const link of items.value) {
      const isValid = path.startsWith(link.path)
      if (isValid) {
        matches.push([link.path.length, link] as [number, NavigationMenuItem])
      }
    }
    currentLink.value = matches.sort((a, b) => b[0] - a[0])[0]?.[1]
  }

  function navigateTo(path: string) {
    router.push(path)
    init(path)
  }
  return { items, navigateTo }
})
