import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'
import ui from '@nuxt/ui/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    vueDevTools(),
    ui({
      ui: {
        colors: {
          primary: 'blue',
          neutral: 'gray',
        },
      },
    }),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  optimizeDeps: {
    include: [
      '@tiptap/vue-3',
      '@tiptap/starter-kit',
      '@tiptap/extension-placeholder',
      '@tiptap/extension-image',
      '@tiptap/extension-link',
      '@tiptap/extension-mention',
      'prosemirror-state',
      'prosemirror-transform',
      'prosemirror-model',
      'prosemirror-view',
      'prosemirror-gapcursor',
      'prosemirror-commands',
      'prosemirror-keymap',
      'prosemirror-inputrules',
      'prosemirror-dropcursor',
      'prosemirror-history',
    ],
  },
})
