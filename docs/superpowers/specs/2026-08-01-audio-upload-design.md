# Audio Upload Design

**Date:** 2026-08-01
**Status:** draft

## Overview

Add audio file upload support to the CMS, allowing authors to upload audio files and embed playable audio players in content. The audio player embeds as raw HTML `<audio>` tags within markdown content.

Approach: custom TipTap `Audio` node extension that renders a native `<audio controls>` element in the editor, serialized as `<audio controls src="url"></audio>` in markdown. The consumer (personal-site) allows `<audio>` tags through `DOMPurify` and renders them as-is.

## Backend Changes

### 1. Validator — `UploadFileCommandValidator.cs`

Add audio MIME types to the allowlist:

```csharp
"audio/mpeg", "audio/wav", "audio/ogg", "audio/mp4",
"audio/webm", "audio/aac", "audio/flac", "audio/x-m4a"
```

Bump max file size from 10MB to 50MB (audio files are typically larger than images).

### 2. Storage — `R2StorageService.cs`

Add `"audio/"` prefix → `"audio"` folder mapping in `GetTypeFolder()`:

```csharp
if (contentType.StartsWith("audio/")) return "audio";
```

### 3. URL Extraction — `ImageUrlExtractor.cs`

Add regex pattern to also extract URLs from `<audio src="...">` tags for file reference tracking. The existing image regex matches `![alt](url)` and `<img src="...">`. Add a third pattern for `<audio[^>]*src="([^"]+)">`:

```csharp
// New pattern alongside existing img/markdown patterns
private static readonly Regex AudioSrcRegex = new(@"<audio[^>]*src=""([^""]+)""", RegexOptions.Compiled);
```

This ensures audio files get auto-linked to posts via `FileReferenceService` and participate in orphan cleanup.

## CMS Frontend Changes

### 1. Custom TipTap Audio Node Extension

New file: `frontend/src/components/editor/AudioExtension.ts`

- Block-level custom node named `audio`
- Attributes: `src` (string, required), `controls` (string, default `"true"`), `autoplay` (string, default `"false"`)
- Renders as native `<audio controls>` element — **playable inline in the editor**
- Serialization:
  - To markdown: `<audio controls src="{src}"></audio>`
  - From markdown: parse `<audio>` tags back into the node
- Parse HTML rule to match `<audio>` elements in pasted or existing markdown

### 2. Dedicated "Insert Audio" toolbar button — `Write.vue`

- New file input with `accept="audio/mpeg,audio/wav,audio/ogg,audio/mp4,audio/webm,audio/aac,audio/flac,audio/x-m4a"`
- Upload flow: file picker → `uploadFile()` → insert Audio node at cursor with R2 URL
- Separate from the existing image button
- Reuses existing `uploadFile()` from `upload.ts`

### 3. `upload.ts`

No changes needed — `uploadFile()` already takes `File` and `altText?`, returns `FileResponse` with `url`.

### 4. `useAudioInsert.ts` composable

New composable (similar to `useImageInsert.ts` but for audio):
- Manages file selection, upload state, and TipTap audio node insertion
- Inserts via `editor.chain().focus().insertContent({ type: 'audio', attrs: { src: url } }).run()`

## Consumer Changes (personal-site)

### 1. DOMPurify config — `Blog.vue`

Add `audio` to `ALLOWED_TAGS` and `src`, `controls` to `ALLOWED_ATTR` in the `DOMPurify.sanitize()` call:

```typescript
DOMPurify.sanitize(serialized, {
  ADD_TAGS: ['audio'],
  ADD_ATTR: ['src', 'controls'],
});
```

### 2. No markdown-it changes

`markdown-it` passes raw HTML through by default (`html: true`). Zero changes needed.

### 3. Optional: audio styling

In the existing `DOMParser` post-processing loop (same pattern used for images), add inline styles to `<audio>` elements (e.g., `maxWidth: '400px', display: 'block', margin: '0 auto'`).

## File Reference Tracking (automatic)

The existing `FileReferenceService.ReconcilePostFilesAsync` scans content for media URLs. After adding `<audio src="...">` regex to `ImageUrlExtractor`, audio file associations are tracked the same as images — deduplicated by content hash, auto-linked to posts, orphan-cleanup after deletion.

## Testing

### Backend
- Unit test: validator accepts audio MIME types and rejects non-allowed types
- Unit test: validator enforces 50MB limit
- Unit test: `GetTypeFolder()` returns `"audio"` for audio content types
- Unit test: `ImageUrlExtractor` extracts URLs from `<audio src="...">` tags

### Frontend
- Verify audio node renders in editor with controls
- Verify audio node serializes/deserializes correctly in markdown
- Verify upload flow end-to-end (file picker → upload → insertion)

### Consumer
- Verify `<audio>` tags survive DOMPurify and render as playable audio

## Open Questions

- Audio size limit: 50MB (confirm before implementation)
