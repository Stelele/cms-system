import { ref } from 'vue'
import type { Editor } from '@tiptap/vue-3'
import { useImageUpload } from './useImageUpload'

export const useImageInsert = () => {
  const { selectedFile, isUploading, handleFileSelect, uploadImage, clearFile, getPreviewUrl } =
    useImageUpload()

  const isImageModalOpen = ref(false)
  let currentEditor: Editor | null = null

  const openImageModal = (editor: Editor) => {
    currentEditor = editor
    clearFile()
    isImageModalOpen.value = true
  }

  const insertImageUrl = (url: string) => {
    if (currentEditor && url) {
      currentEditor.chain().focus().setImage({ src: url }).run()
      closeModal()
    }
  }

  const insertImageFile = async (): Promise<string | null> => {
    if (!currentEditor || !selectedFile.value) return null

    const url = await uploadImage()
    if (currentEditor && url) {
      currentEditor.chain().focus().setImage({ src: url }).run()
      closeModal()
    }
    return url
  }

  const closeModal = () => {
    isImageModalOpen.value = false
    clearFile()
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
    getPreviewUrl,
  }
}
