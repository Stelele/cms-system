import dayjs from 'dayjs'

export function stripMarkdown(content: string | undefined | null): string {
  const plainText = (content ?? '')
    .replace(/[#*`_~[\]]/g, '')
    .replace(/\n+/g, ' ')
    .trim()
  return plainText.length > 150 ? plainText.slice(0, 150) + '...' : plainText
}

export function formatDate(dateStr: string | undefined | null): string {
  if (!dateStr) return ''
  return dayjs(dateStr).format('MMM D, YYYY')
}
