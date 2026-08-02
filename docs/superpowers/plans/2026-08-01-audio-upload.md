# Audio Upload Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add audio file upload support to the CMS, allowing authors to upload audio files and embed playable `<audio>` players in content via a dedicated toolbar button.

**Architecture:** Custom TipTap `Audio` node extension renders a native `<audio controls>` element in the editor, serializes as HTML in markdown. Backend validator expanded for audio MIME types with 50MB limit. `ImageUrlExtractor` updated to also parse `<audio src="...">` for automatic file reference tracking. Consumer (personal-site) configured to allow `<audio>` through DOMPurify.

**Tech Stack:** .NET 10 (C#), TipTap Vue 3 (via @nuxt/ui UEditor), @tiptap/markdown, DOMPurify

---

### Task 1: Update upload validator — audio MIME types + size limit

**Files:**
- Modify: `backend/Application/Files/UploadFileCommandValidator.cs`

- [ ] **Step 1: Add audio MIME types and bump size to 50MB**

Replace the existing `AllowedContentTypes` set and `MaxFileSizeBytes` constant with the expanded versions:

```csharp
private static readonly HashSet<string> AllowedContentTypes =
[
    "image/jpeg",
    "image/png",
    "image/gif",
    "image/webp",
    "image/svg+xml",
    "image/bmp",
    "image/tiff",
    "image/x-icon",
    "image/apng",
    "image/avif",
    "image/x-xbitmap",
    "audio/mpeg",
    "audio/wav",
    "audio/ogg",
    "audio/mp4",
    "audio/webm",
    "audio/aac",
    "audio/flac",
    "audio/x-m4a",
];

private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50MB
```

- [ ] **Step 2: Build and verify**

```bash
dotnet build
```

Expected: Build succeeds, no errors.

- [ ] **Step 3: Commit**

```bash
git add backend/Application/Files/UploadFileCommandValidator.cs
git commit -m "feat: add audio MIME types and bump upload limit to 50MB"
```

---

### Task 2: Add audio type folder to R2 storage

**Files:**
- Modify: `backend/Infrastructure/Services/R2StorageService.cs`

- [ ] **Step 1: Add audio folder mapping in GetTypeFolder()**

After the `contentType.StartsWith("video/")` check (line 102), add an audio check before the GIF/image/image checks:

```csharp
if (contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
    return "audio";
```

The resulting method should be:

```csharp
public static string GetTypeFolder(string contentType)
{
    if (string.IsNullOrEmpty(contentType))
        return "other";

    if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
        return "videos";

    if (contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
        return "audio";

    if (string.Equals(contentType, "image/gif", StringComparison.OrdinalIgnoreCase))
        return "gifs";

    if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        return "images";

    return "other";
}
```

- [ ] **Step 2: Build and verify**

```bash
dotnet build
```

Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add backend/Infrastructure/Services/R2StorageService.cs
git commit -m "feat: add 'audio' type folder in R2 storage"
```

---

### Task 3: Add audio tag URL extraction for file reference tracking

**Files:**
- Modify: `backend/Infrastructure/Services/ImageUrlExtractor.cs`

- [ ] **Step 1: Add audio URL extraction regex and loop**

Add a new source-generated regex and extraction loop for `<audio src="...">`:

```csharp
using System.Text.RegularExpressions;

namespace Infrastructure.Services;

public static partial class ImageUrlExtractor
{
    [GeneratedRegex(@"!\[.*?\]\(([^\s""']+)(?:\s+""[^""]*"")?\)")]
    private static partial Regex MarkdownImageRegex();

    [GeneratedRegex(@"<img[^>]*\bsrc\s*=\s*[""']([^""']+)[""'][^>]*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlImageRegex();

    [GeneratedRegex(@"<audio[^>]*\bsrc\s*=\s*[""']([^""']+)[""'][^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlAudioRegex();

    public static List<string> ExtractImageUrls(string? content, string? coverImageUrl, string publicBucketUrlPrefix)
    {
        var urls = new List<string>();

        if (!string.IsNullOrEmpty(coverImageUrl) && coverImageUrl.StartsWith(publicBucketUrlPrefix))
            urls.Add(coverImageUrl);

        if (!string.IsNullOrEmpty(content))
        {
            foreach (Match match in MarkdownImageRegex().Matches(content))
                if (match.Groups[1].Value.StartsWith(publicBucketUrlPrefix))
                    urls.Add(match.Groups[1].Value);

            foreach (Match match in HtmlImageRegex().Matches(content))
                if (match.Groups[1].Value.StartsWith(publicBucketUrlPrefix))
                    urls.Add(match.Groups[1].Value);

            foreach (Match match in HtmlAudioRegex().Matches(content))
                if (match.Groups[1].Value.StartsWith(publicBucketUrlPrefix))
                    urls.Add(match.Groups[1].Value);
        }

        return urls;
    }
}
```

- [ ] **Step 2: Build and verify**

```bash
dotnet build
```

Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add backend/Infrastructure/Services/ImageUrlExtractor.cs
git commit -m "feat: extract audio URLs from <audio src> for file reference tracking"
```

---

### Task 4: Create custom TipTap Audio node extension

**Files:**
- Create: `frontend/src/components/editor/AudioExtension.ts`

- [ ] **Step 1: Create audio extension directory**

```bash
mkdir -p frontend/src/components/editor
```

- [ ] **Step 2: Write the Audio extension**

Create `frontend/src/components/editor/AudioExtension.ts`:

```typescript
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

  renderMarkdown(state, node) {
    const src = node.attrs.src || ''
    return `<audio controls src="${src}"></audio>`
  },
})
```

- [ ] **Step 3: Run type check**

```bash
npm run type-check
```

Expected: No TypeScript errors.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/components/editor/AudioExtension.ts
git commit -m "feat: add custom TipTap audio node extension"
```

---

### Task 5: Create useAudioInsert composable

**Files:**
- Create: `frontend/src/composables/useAudioInsert.ts`

- [ ] **Step 1: Write the composable**

Create `frontend/src/composables/useAudioInsert.ts`:

```typescript
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
```

- [ ] **Step 2: Run type check**

```bash
npm run type-check
```

Expected: No errors.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/composables/useAudioInsert.ts
git commit -m "feat: add useAudioInsert composable for audio upload + insertion"
```

---

### Task 6: Add Insert Audio button to Write.vue

**Files:**
- Modify: `frontend/src/views/Write.vue`

- [ ] **Step 1: Add audio extension to UEditor**

In the `UEditor` component, add the `:extensions` prop. Modify the `<UEditor>` tag (around line 121-128) to include the audio extension:

```vue
<UEditor
  v-slot="{ editor }"
  v-model="editorContent"
  content-type="markdown"
  :placeholder="{ placeholder: 'Start writing your article...', mode: 'firstLine' }"
  :image="true"
  :extensions="editorExtensions"
  class="min-h-[500px]"
  @update="handleEditorUpdate"
>
```

- [ ] **Step 2: Add "Insert Audio" popover button next to "Insert Image"**

After the closing `</UPopover>` for the image insert (line 200-201), add a matching audio popover:

```vue
<UPopover
  :open="isAudioModalOpen"
  @update:open="isAudioModalOpen = $event"
  class="w-96"
>
  <UButton
    variant="ghost"
    size="sm"
    icon="i-lucide-music"
    @click.stop="openAudioModal(editor)"
  >
    Insert Audio
  </UButton>
  <template #content>
    <div class="p-4 space-y-4">
      <h2 class="text-lg font-semibold">Insert Audio</h2>
      <p class="text-sm text-muted">Enter an audio URL or select a local file</p>
      <div class="space-y-4">
        <UFormField label="Audio URL" name="audioUrl">
          <UInput
            v-model="localAudioUrl"
            placeholder="https://example.com/audio.mp3"
            class="w-full"
          />
        </UFormField>

        <UFormField label="Or Upload File" name="audioFileUpload">
          <UInput
            type="file"
            accept="audio/mpeg,audio/wav,audio/ogg,audio/mp4,audio/webm,audio/aac,audio/flac,audio/x-m4a"
            class="w-full"
            @change="handleAudioFileSelect"
          />
        </UFormField>

        <div v-if="selectedAudioFile" class="flex items-center gap-4">
          <span class="text-sm text-muted">{{ selectedAudioFile.name }}</span>
        </div>
      </div>
      <div class="flex justify-end gap-3 mt-4">
        <UButton color="neutral" variant="outline" size="sm" @click="closeAudioModal">
          Cancel
        </UButton>
        <UButton
          v-if="localAudioUrl"
          :loading="isAudioUploading"
          size="sm"
          @click="insertAudioUrl"
        >
          Insert from URL
        </UButton>
        <UButton
          v-if="selectedAudioFile"
          :loading="isAudioUploading"
          size="sm"
          @click="insertAudioFileHandler"
        >
          Insert File
        </UButton>
      </div>
    </div>
  </template>
</UPopover>
```

- [ ] **Step 3: Add script imports, extension array, and composable wiring**

In the `<script setup>` section (around line 211-253):

Add the import for the audio composable and extension:

```typescript
import { useAudioInsert } from '@/composables/useAudioInsert'
import { AudioExtension } from '@/components/editor/AudioExtension'
```

Add the extension array:

```typescript
const editorExtensions = [AudioExtension]
```

Add all composable destructured properties (after the existing `useImageInsert` destructuring at line 234-244):

```typescript
const {
  isAudioModalOpen,
  selectedFile: selectedAudioFile,
  isUploading: isAudioUploading,
  openAudioModal,
  insertAudioUrl: insertAudioUrlFromModal,
  handleFileSelect: handleAudioFileSelect,
  insertAudioFile: insertAudioFileFromModal,
  closeModal: closeAudioModal,
} = useAudioInsert()
```

Add the reactive local state:

```typescript
const localAudioUrl = ref('')
```

Add the action functions:

```typescript
const insertAudioUrl = () => {
  if (localAudioUrl.value) {
    insertAudioUrlFromModal(localAudioUrl.value)
  }
}

async function insertAudioFileHandler() {
  await insertAudioFileFromModal()
}
```

- [ ] **Step 4: Run lint and type check**

```bash
npm run lint
npm run type-check
```

Expected: No errors from either command.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/views/Write.vue
git commit -m "feat: add Insert Audio button with upload and URL insertion in Write.vue"
```

---

### Task 7: Update consumer Blog.vue — allow audio in DOMPurify + style audio elements

**Files:**
- Modify: `frontend/src/pages/blog/Blog.vue` (in the **personal-site** project at `/home/gift/Documents/code-projects/personal-site`)

- [ ] **Step 1: Allow `<audio>` tag in DOMPurify sanitize call**

In the `augmentedContent` computed (line 138), change:

```typescript
return DOMPurify.sanitize(serialized);
```

to:

```typescript
return DOMPurify.sanitize(serialized, {
  ADD_TAGS: ['audio'],
  ADD_ATTR: ['src', 'controls'],
});
```

- [ ] **Step 2: Add audio styling in the DOMParser post-processing loop**

In the `augmentedContent` computed, add audio element styling after the existing image/figcaption loops (after line 134 `caption.style.textAlign = "center";`):

```typescript
doc.querySelectorAll("audio").forEach((audio) => {
  audio.style.display = "block";
  audio.style.marginLeft = "auto";
  audio.style.marginRight = "auto";
  audio.style.maxWidth = "400px";
  audio.style.width = "100%";
});
```

- [ ] **Step 3: Verify TypeScript**

```bash
npm run type-check
```

Expected: No errors.

- [ ] **Step 4: Commit** (in the personal-site repo)

```bash
git add frontend/src/pages/blog/Blog.vue
git commit -m "feat: allow <audio> tags in DOMPurify and style audio elements"
```

---

### Verification

- [ ] **Manual smoke test:**
  1. Start backend: `dotnet run --project backend/Host/Host.csproj`
  2. Start CMS frontend: `cd frontend && npm run dev`
  3. Go to Write page, click "Insert Audio", upload an MP3
  4. Verify audio player renders in the editor and is playable
  5. Save the post, check the markdown content contains `<audio controls src="...">`
  6. Start personal-site: `cd /home/gift/Documents/code-projects/personal-site/frontend && npm run dev`
  7. View the blog post, verify audio player appears and is playable
