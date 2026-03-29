import { ref } from 'vue'

const GROQ_API_URL = 'https://api.groq.com/openai/v1/chat/completions'
const GROQ_MODEL = 'llama-3.3-70b-versatile'

const PROMPT_TEMPLATE = `You are an expert content curator for a blog. Given an article, generate a concise, compelling 1-2 sentence description (tagline) that would make someone want to read the article. Focus on the core insight or value proposition. Do not start with phrases like "This article explores", "In this article", or "Here is the tagline:".`

function cleanContent(content: string): string {
  return content
    .replace(/[#*`_~[\]]/g, '')
    .replace(/\n+/g, ' ')
    .trim()
}

export function useArticleSummarizer() {
  const isLoading = ref(false)
  const isSummarizing = ref(false)
  const error = ref<string | null>(null)

  async function summarize(content: string): Promise<string> {
    isSummarizing.value = true
    error.value = null

    const clean = cleanContent(content)

    try {
      const apiKey = import.meta.env.VITE_GROQ_API_KEY
      if (!apiKey) {
        throw new Error('Groq API key not configured. Set VITE_GROQ_API_KEY in your environment.')
      }

      const response = await fetch(GROQ_API_URL, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${apiKey}`,
        },
        body: JSON.stringify({
          model: GROQ_MODEL,
          messages: [
            { role: 'system', content: PROMPT_TEMPLATE },
            { role: 'user', content: `Article text:\n\n${clean}` },
          ],
          max_tokens: 100,
          temperature: 0.7,
        }),
      })

      if (!response.ok) {
        const errBody = await response.json().catch(() => ({}))
        throw new Error(errBody?.error?.message ?? `Groq API error: ${response.status}`)
      }

      const data = await response.json()
      const summary = data.choices?.[0]?.message?.content?.trim() ?? ''

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
