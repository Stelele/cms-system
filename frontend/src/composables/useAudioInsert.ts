import { ref } from 'vue'
import type { Editor } from '@tiptap/vue-3'
import { useImageUpload } from './useImageUpload'

export const useAudioInsert = () => {
  const { selectedFile, isUploading, handleFileSelect, uploadImage, clearFile } =
    useImageUpload()

  const isAudioModalOpen = ref(false)
  let currentEditor: Editor | null = null

  const openAudioModal = (editor: Editor) => {
    currentEditor = editor
    clearFile()
    isAudioModalOpen.value = true
  }

  const insertAudioUrl = (url: string) => {
    if (currentEditor && url) {
      currentEditor.chain().focus().setAudio({ src: url }).run()
      closeModal()
    }
  }

  const insertAudioFile = async (): Promise<string | null> => {
    if (!currentEditor || !selectedFile.value) return null

    const url = await uploadImage()
    if (currentEditor && url) {
      currentEditor.chain().focus().setAudio({ src: url }).run()
      closeModal()
    }
    return url
  }

  const closeModal = () => {
    isAudioModalOpen.value = false
    clearFile()
    currentEditor = null
  }

  return {
    isAudioModalOpen,
    selectedFile,
    isUploading,
    openAudioModal,
    insertAudioUrl,
    handleFileSelect,
    insertAudioFile,
    closeModal,
  }
}
