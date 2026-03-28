import { ref } from 'vue'
import { uploadFile } from '@/services/upload'

export const useImageUpload = () => {
  const selectedFile = ref<File | null>(null)
  const isUploading = ref(false)

  const handleFileSelect = (event: Event) => {
    const target = event.target as HTMLInputElement
    if (target.files && target.files[0]) {
      selectedFile.value = target.files[0]
    }
  }

  const uploadImage = async (): Promise<string | null> => {
    if (!selectedFile.value) return null

    isUploading.value = true
    let url: string | null = null
    
    try {
      const result = await uploadFile(selectedFile.value)
      url = `${import.meta.env.VITE_API_URL}${result.url}`
    } catch (err) {
      console.error('Image upload failed:', err)
    } finally {
      isUploading.value = false
    }

    return url
  }

  const clearFile = () => {
    selectedFile.value = null
  }

  const getPreviewUrl = (): string | undefined => {
    if (!selectedFile.value) return undefined
    return URL.createObjectURL(selectedFile.value)
  }

  return {
    selectedFile,
    isUploading,
    handleFileSelect,
    uploadImage,
    clearFile,
    getPreviewUrl,
  }
}
