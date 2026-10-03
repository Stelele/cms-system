# images-to-spritesheet

An Electron + Vue 3 app that walks a folder of PNGs, auto-crops transparent edges, bin-packs them into one sheet, and writes it plus a TexturePacker-style JSON manifest.

## What it does

Pick an input folder and output in two native dialogs, press **Create Spritesheet**, and the app collects every `*.png`, trims each transparent border, packs them, and writes `<output>.png` plus `<output>.json`. While it runs the form is a skeleton; on completion it shows a success block with both paths or an `Errror:`.

## How it works

Two Vite builds glued by a hand-written plugin.

- `vite.config.ts` (99 lines) builds the renderer; its `electron-vite` plugin hooks `configureServer` to start a second build via `vite.config.electron.ts`, watch it, and `spawn(electron, [electronMain])`, restarting it on every change. It bundles `main.ts`, `preload.ts` and `worker.ts` as an SSR build (`emptyOutDir: false`).
- `src-electron/main.ts` (23 lines) calls `createWindow()` and `mapHandlers()` from `appFuncs.ts`, which builds the `BrowserWindow` with `contextIsolation: true` and a dev/packaged preload path, then registers three `ipcMain.handle` channels — `dialog:select-dir`, `dialog:save-file`, `spriteSheet`. `src-electron/preload.ts` (6 lines) exposes exactly those as `window.electron.{openFolderSelect,openFileSave,spriteSheet}` — the entire attack surface.
- `funcs.ts` (37 lines) implements the two dialogs and `imagesToSpriteSheet()`, spawning a `worker_threads` `Worker` from `new URL("worker.js", import.meta.url)` wrapped in a `Promise` on `message`/`error`/non-zero `exit`.
- `src-electron/worker.ts` (166 lines) does the work: `glob(`${dirPath}/**/*.png`).sort()`, then `spriteSheet()` loads them via `loadImage`, crops with `detect-edges`, packs with `bin-pack` (1px margin), draws onto a canvas, and emits `meta` + `frames` entries with `frame`, `trimmed`, `spriteSourceSize`, `sourceSize`.
- `src/App.vue` (95 lines) is the only renderer file: two gated inputs, one button, one `<pre>` status block.

### Stack

- **Electron 33 + Forge** shell and packaging (`forge.config.cjs`: squirrel, zip, deb, rpm).
- **Vue 3 `<script setup>` + Vite 5 + TypeScript** UI; **Tailwind 3 + daisyUI** styling.
- **skia-canvas** (native, hence `plugin-auto-unpack-natives`) rasterises, **detect-edges** trims, **bin-pack** packs, **glob** scans, **worker_threads** keeps it off the main process.

## What works well

- Security is deliberate: `contextIsolation: true`, a 6-line preload exposing exactly three functions, no `nodeIntegration`, and `FusesPlugin` flipping `RunAsNode`/`EnableNodeCliInspectArguments` off.
- Cropping and packing run in a worker thread (`funcs.ts` → `worker.ts`), so hundreds of images never block the renderer.
- The JSON is a real TexturePacker-compatible manifest — `meta.app`, `meta.size`, per-frame `frame`/`rotated`/`trimmed`/`spriteSourceSize`/`sourceSize` — dropping into an existing engine.
- The dev loop: edit `worker.ts`, Vite rebuilds, the Electron child restarts — all from `npm run dev`.

## What I'd change

- **The Electron half is outside the type checker.** `tsconfig.app.json` includes only `src/**/*.ts` and `tsconfig.node.json` includes only `vite.config.ts`, so `vue-tsc -b` — the first thing `npm run build` runs — never looks at `src-electron/main.ts`, `funcs.ts`, `appFuncs.ts`, `worker.ts` or `preload.ts`.
- **`src/types.d.ts:11` declares `spriteSheet: (…) => Promise<string?>`.** `?` in a type position is a syntax error; `tsc` rejects it with `TS17019` ("'?' at the end of a type is not valid TypeScript syntax") even under `skipLibCheck`. The declared renderer→main API never parses.
- **Write errors are swallowed and success is reported anyway.** `worker.ts` does `fs.writeFile(…, (err) => (console.error(err)))` for both outputs and then unconditionally returns `{ succeded: true, imageLoc, jsonLoc }`. A full disk looks like success in the UI.
- **The error branch in `App.vue` is unreachable.** `funcs.ts` *rejects* the promise on worker failure, so `const response = await window.electron.spriteSheet(…)` throws, `if (response.succeded)` never runs, `isProccessing.value = false` never runs, and the UI is stuck on the skeleton forever. `types.d.ts` promises a `{ succeded: false, error }` shape nothing ever produces.
- **Paths are Windows-only in two places.** The manifest keys are built with `item.source.src.replace(`${prefix}\\`, "")` — on Linux/macOS the backslash never matches, so keys keep the absolute path. And the output is written to `` `${output}.png` `` where `output` is already the full path chosen in the save dialog, giving `sheet.png.png`.
- Input globbing is `**/*.png` only, even though `spriteSheet()` validates `outputFormat` against `["png", "jpeg"]` — JPEG input is silently skipped.

## Still outstanding

- **No tests and no lint/format config anywhere** — no `test` script in `package.json`, no vitest/eslint config, no `*.spec.ts`.
- `README.md` is still the untouched Vite starter text ("Vue 3 + TypeScript + Vite…"), which does not mention Electron, the worker, or what the tool outputs.
- The single FIXME in the codebase, `worker.ts:54` — `// FIXME: can read JSON module when supported` — is why `homepage` and `version` are hardcoded strings rather than read from `package.json`.
- `defaultOptions` in `worker.ts` advertises `margin`, `crop` and `outputFormat`, but `imagesToSpriteSheet()` overrides them with its own literal and there is no UI to change them.
- Typos survive in the user-facing strings: `isProccessing`, `sprteOut`, and `Errror:` in `App.vue`.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
