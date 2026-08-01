import { authFetch } from '@/services/auth-fetch'

export interface FileResponse {
  id: string
  fileName: string
  url: string
  contentType: string
  size: number
  altText: string | null
  isNew: boolean
}

export const uploadFile = async (file: File, altText?: string): Promise<FileResponse> => {
  const formData = new FormData()
  formData.append('file', file)
  if (altText) {
    formData.append('altText', altText)
  }

  const response = await authFetch(`${import.meta.env.VITE_API_URL}/files/upload`, {
    method: 'POST',
    body: formData,
  })

  if (!response.ok) {
    const errorData = await response.json().catch(() => ({ error: { message: 'Upload failed' } }))
    throw new Error(errorData.error?.message ?? `Upload failed: ${response.statusText}`)
  }

  return response.json()
}

export const associateFileWithPost = async (fileId: string, postId: string): Promise<void> => {
  const response = await authFetch(
    `${import.meta.env.VITE_API_URL}/files/${fileId}/posts/${postId}`,
    { method: 'POST' },
  )

  if (!response.ok && response.status !== 204) {
    const errorData = await response.json().catch(() => ({ error: { message: 'Association failed' } }))
    throw new Error(errorData.error?.message ?? `Association failed: ${response.statusText}`)
  }
}

export const getFilesByPost = async (postId: string): Promise<FileResponse[]> => {
  const response = await authFetch(`${import.meta.env.VITE_API_URL}/posts/${postId}/files`, {
    method: 'GET',
    headers: {
      Accept: 'application/json',
    },
  })

  if (!response.ok) {
    throw new Error(`Failed to fetch files: ${response.statusText}`)
  }

  return response.json()
}
