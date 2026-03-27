import { useAuthStore } from '@/stores/auth-store'

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
  const authStore = useAuthStore()
  const formData = new FormData()
  formData.append('file', file)
  if (altText) {
    formData.append('altText', altText)
  }

  const response = await fetch(`${import.meta.env.VITE_API_URL}/files/upload`, {
    method: 'POST',
    body: formData,
    headers: {
      Authorization: `Bearer ${authStore.accessToken}`,
    },
  })

  if (!response.ok) {
    const errorData = await response.json().catch(() => ({ error: { message: 'Upload failed' } }))
    throw new Error(errorData.error?.message ?? `Upload failed: ${response.statusText}`)
  }

  return response.json()
}

export const associateFileWithPost = async (fileId: string, postId: string): Promise<void> => {
  const authStore = useAuthStore()

  const response = await fetch(
    `${import.meta.env.VITE_API_URL}/files/${fileId}/posts/${postId}`,
    {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${authStore.accessToken}`,
      },
    },
  )

  if (!response.ok && response.status !== 204) {
    const errorData = await response.json().catch(() => ({ error: { message: 'Association failed' } }))
    throw new Error(errorData.error?.message ?? `Association failed: ${response.statusText}`)
  }
}

export const getFilesByPost = async (postId: string): Promise<FileResponse[]> => {
  const authStore = useAuthStore()

  const response = await fetch(`${import.meta.env.VITE_API_URL}/posts/${postId}/files`, {
    method: 'GET',
    headers: {
      Authorization: `Bearer ${authStore.accessToken}`,
      Accept: 'application/json',
    },
  })

  if (!response.ok) {
    throw new Error(`Failed to fetch files: ${response.statusText}`)
  }

  return response.json()
}
