import { ref } from 'vue'
import { BackendApiSingleton } from '@/services/backend'

export function useArticleSummarizer() {
  const isLoading = ref(false)
  const isSummarizing = ref(false)
  const error = ref<string | null>(null)

  async function summarize(content: string): Promise<string> {
    isSummarizing.value = true
    error.value = null

    try {
      const client = await BackendApiSingleton.getInstance()
      const { data, error: apiError } = await client.POST('/summarize', {
        body: { content },
      })

      if (apiError) {
        const message =
          'status' in apiError
            ? `Failed to generate summary (${apiError.status})`
            : 'Failed to generate summary'
        error.value = message
        isSummarizing.value = false
        throw new Error(message)
      }

      const summary = data?.summary ?? ''

      isSummarizing.value = false
      return summary
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to generate summary'
      error.value = message
      isSummarizing.value = false
      throw new Error(message)
    }
  }

  return {
    summarize,
    isLoading,
    isSummarizing,
    error,
  }
}
