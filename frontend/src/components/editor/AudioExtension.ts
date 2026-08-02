import { Node } from '@tiptap/core'

export interface AudioOptions {
  HTMLAttributes: Record<string, unknown>
}

declare module '@tiptap/core' {
  interface Commands<ReturnType> {
    audio: {
      setAudio: (options: { src: string }) => ReturnType
    }
  }
}

export const AudioExtension = Node.create<AudioOptions>({
  name: 'audio',

  group: 'block',

  atom: true,

  inline: false,

  draggable: true,

  addOptions() {
    return {
      HTMLAttributes: {},
    }
  },

  addAttributes() {
    return {
      src: {
        default: null,
      },
    }
  },

  parseHTML() {
    return [
      {
        tag: 'audio',
        getAttrs: (node) => ({
          src: (node as HTMLElement).getAttribute('src'),
        }),
      },
    ]
  },

  renderHTML({ HTMLAttributes }) {
    return ['audio', { controls: '', ...HTMLAttributes }]
  },

  addCommands() {
    return {
      setAudio:
        (options) =>
        ({ commands }) => {
          return commands.insertContent({
            type: this.name,
            attrs: options,
          })
        },
    }
  },

  renderMarkdown: (node) => {
    const src = (node as { attrs?: { src?: string } }).attrs?.src || ''
    return `<audio controls src="${src}"></audio>`
  },

  parseMarkdown: (token, helpers) => {
    const raw = (token as { raw?: string; text?: string }).raw
      || (token as { raw?: string; text?: string }).text
      || ''
    const match = /<audio[^>]*\bsrc\s*=\s*["']([^"']+)["'][^>]*>/i.exec(raw)
    if (match) {
      return helpers.createNode('audio', { src: match[1] })
    }
    return helpers.createNode('paragraph') as ReturnType<typeof helpers.createNode>
  },
})
