import { ref } from 'vue'
import type { Editor } from '@tiptap/vue-3'
import { uploadFile } from '@/services/upload'

export const useImageInsert = () => {
  const isImageModalOpen = ref(false)
  const selectedFile = ref<File | null>(null)
  const isUploading = ref(false)
  let currentEditor: Editor | null = null

  const openImageModal = (editor: Editor) => {
    currentEditor = editor
    selectedFile.value = null
    isImageModalOpen.value = true
  }

  const insertImageUrl = (url: string) => {
    if (currentEditor && url) {
      currentEditor.chain().focus().setImage({ src: url }).run()
      closeModal()
    }
  }

  const handleFileSelect = (event: Event) => {
    const target = event.target as HTMLInputElement
    if (target.files && target.files[0]) {
      selectedFile.value = target.files[0]
    }
  }

  const insertImageFile = async (): Promise<string | null> => {
    if (!currentEditor || !selectedFile.value) return null

    isUploading.value = true
    try {
      const result = await uploadFile(selectedFile.value)
      const url = `${import.meta.env.VITE_API_URL}${result.url}`
      if (currentEditor && url) {
        currentEditor.chain().focus().setImage({ src: url }).run()
        closeModal()
      }
      return url
    } catch (err) {
      console.error('Image upload failed:', err)
      return null
    } finally {
      isUploading.value = false
    }
  }

  const closeModal = () => {
    isImageModalOpen.value = false
    selectedFile.value = null
    currentEditor = null
  }

  return {
    isImageModalOpen,
    selectedFile,
    isUploading,
    openImageModal,
    insertImageUrl,
    handleFileSelect,
    insertImageFile,
    closeModal,
  }
}
