import { ref } from 'vue'
import type { Editor } from '@tiptap/vue-3'

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
    console.log('insertImageUrl called', { currentEditor: !!currentEditor, url })
    if (currentEditor && url) {
      console.log('Inserting image:', url)
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

  const insertImageFile = async () => {
    if (!currentEditor || !selectedFile.value) return

    isUploading.value = true
    try {
      const reader = new FileReader()
      reader.onload = () => {
        if (currentEditor && reader.result) {
          currentEditor.chain().focus().setImage({ src: reader.result as string }).run()
          closeModal()
        }
      }
      reader.readAsDataURL(selectedFile.value)
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
