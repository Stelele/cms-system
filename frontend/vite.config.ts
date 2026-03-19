import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import ui from "@nuxt/ui/vite";
import path from "node:path";

export default defineConfig({
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "src"),
    },
  },
  plugins: [
    vue(),
    ui({
      ui: {
        pageGrid: {
          base: "relative grid grid-cols-6 sm:grid-cols-2 md:grid-cols-6 lg:grid-cols-6 gap-4",
        },
        table: {
          slots: {
            th: "px-3.5 py-3.5 text-lg text-highlighted text-left rtl:text-right font-semibold [&:has([role=checkbox])]:pe-0",
            td: "p-3.5 text-lg text-normal whitespace-nowrap [&:has([role=checkbox])]:pe-0",
          },
        },
      },
    }),
  ],
});
