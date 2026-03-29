import { defineStore } from 'pinia'
import { ref, watch } from 'vue'

const STORAGE_KEY = 'cms-settings'

interface Settings {
  aiSummarizationEnabled: boolean
}

function loadSettings(): Settings {
  try {
    const stored = localStorage.getItem(STORAGE_KEY)
    if (stored) {
      return JSON.parse(stored)
    }
  } catch {
    // Ignore localStorage errors
  }
  return { aiSummarizationEnabled: true }
}

function saveSettings(settings: Settings) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(settings))
  } catch {
    // Ignore localStorage errors
  }
}

export const useSettingsStore = defineStore('settingsStore', () => {
  const settings = ref<Settings>(loadSettings())

  const aiSummarizationEnabled = ref(settings.value.aiSummarizationEnabled)

  watch(aiSummarizationEnabled, (value) => {
    settings.value.aiSummarizationEnabled = value
    saveSettings(settings.value)
  })

  function toggleAiSummarization() {
    aiSummarizationEnabled.value = !aiSummarizationEnabled.value
  }

  return {
    aiSummarizationEnabled,
    toggleAiSummarization,
  }
})
