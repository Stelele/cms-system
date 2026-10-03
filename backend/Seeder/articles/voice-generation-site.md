# voice-generation-site

A browser-based text-to-speech reader ("Cloud Reader") that drives Microsoft Edge's Read Aloud endpoint over WebSocket, with per-word highlighting as the audio plays.

## What it does

The home route (`/`) is a marketing page with a language dropdown and a grid of four voice cards per locale; clicking a card fetches a random dad joke from `icanhazdadjoke.com`, synthesises it in that voice and plays it as a preview. The `/reader` route is the actual product: an editable text area (per-paragraph `contenteditable` blocks), a voice picker modal, pitch/rate sliders, and a play button. Pressing play sends the whole text to the TTS endpoint, builds an `Audio` element from the base64 result, and on every `timeupdate` highlights the current paragraph and the current word in the DOM.

## How it works

`src/App.vue` fetches the voice list on mount (`TtsService.getVoices()` → `EdgeTTS.getVoices()`), maps each voice to a static avatar at `/voices-faces/<ShortName>.png` (322 PNGs in `public/`), and derives the locale list. `src/services/TtsService/EdgeTTS.ts` (198 lines) is the core: it opens `wss://speech.platform.bing.com/consumer/speech/synthesize/readaloud/edge/v1` with the hard-coded `TRUSTED_CLIENT_TOKEN`, sends a `speech.config` message requesting `audio-24khz-48kbitrate-mono-mp3`, then an SSML message built by `getSSML()` embedding pitch/rate/volume. Binary frames are collected in `message_stream`, stripped of the `Path:audio` header in `processMessageData()`, concatenated and base64-encoded; `Path:audio.metadata` frames feed `subtitle_stream`, which carries word boundaries.

`TtsService.ts` wraps this as a static method, clamping rate/volume/pitch to ±99 and returning `data:audio/x-wav;base64,…`. `ReaderStore.ts` (94 lines, Pinia) holds the text, options, voices and the `timeSlices` computed, which converts word offsets (`(Offset + Duration) * 100e-9`) into `wordSlices` and then groups them into per-line `sentenceSlices` by splitting the local text on `\n`. Playback and highlighting live in `TopAppBar.vue` (185 lines).

### Stack

Vue 3.5 + TypeScript + Vite 6, Pinia, vue-router, Tailwind CSS v4 + daisyUI, `@vueuse/*`, `oh-vue-icons`. Linting via ESLint plus oxlint; `vue-tsc` type-checks in `npm run build`. `.github/workflows/deploy.yml` builds and force-pushes `dist/` into a separate `voice-generation-site-built` repo using a `PAT` secret, copying `index.html` to `404.html` for GitHub Pages.

## What works well

- The whole TTS client was ported from a Node library to run against the browser WebSocket API, including header parsing (`findStart`) and buffering — it works with no backend of its own.
- Word-level karaoke highlighting is derived from real boundary metadata rather than guessed timing, and `remap()` handles degenerate slider ranges.
- Type-checking, two linters and a deploy workflow are wired up in `package.json`; this is better tooled than most side projects.

## What I'd change

- `TopAppBar.vue` is doing four jobs: toolbar, playback engine, audio lifecycle and DOM highlighting, via repeated `innerHTML` writes (lines 136–163). It should be a composable.
- `playAudio()` has no `try/catch` around `await TtsService.getBase64StrAudio(...)` (line 112). If the socket errors, the promise rejects, `midButtons[1].disabled` stays `true` and the play button is dead for the session.
- The data URL says `audio/x-wav` (`TtsService.ts:24`) but the endpoint returns MP3.
- `timeSlices` reads `words[0].minTime` (ReaderStore.ts:67) — an empty paragraph (which the Enter-key handler happily creates) makes `words` empty and throws during `timeupdate`.
- `EdgeTTS.ts:13` hard-codes Microsoft's trusted client token and an undocumented endpoint; a token rotation or ToS change breaks the app with no fallback.

## Still outstanding

- No tests whatsoever: no test script, no spec files, no CI test step — only `type-check` and lint.
- README is the untouched Vue starter template (`# .`), documenting nothing about the project.
- Dead UI: the restart/skip buttons in `midButtons` have no `onClick`, and the CC/clipboard/save/search/more icons plus the modal's search button are decorative.
- `FriendlyName` is mutated in place inside two `computed`s, so repeated renders re-run the string surgery on already-transformed values.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
