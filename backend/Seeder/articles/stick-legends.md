# stick-legends

An early-stage 2D fighting-game prototype: a hand-written WebGPU renderer that loads an Aseprite spritesheet and plays its animations, in a desktop shell.

## What it does

`npm run dev` serves a black full-window page; `npm start` opens it in NW.js (800×600 config in `package.json`). A single fighter — `"monk"`, from `public/ground_monk.png` and `.json` — stands at 240×240 and cycles animations: five loops each, then `Fighter` advances to the next name. The canvas is a 16:9 letterbox sized from the window.

There is no gameplay yet: no movement, no opponent, no input handling anywhere in `src/`.

## How it works

`src/main.ts` (44 lines) is the entry point. It defines `gameWorld` (`{ width: 1280, height: 720, depth: 10 }`), builds a `<canvas>`, constructs a `RendererManager`, awaits `renderer.init(canvas)`, creates one `Fighter`, and runs `requestAnimationFrame(gameLoop)`, passing `dt` in seconds to `renderer.render(dt, gameObjects)`.

`src/GameObjects.ts` defines the model as plain interfaces — `GameObject` with `pos`, `size`, `image?`, `animations`, `curAnimation`, `init`/`update`; `Animation` is a list of `{x, y, w, h}` frames plus a `duration`.

`src/AsespriteExport.ts` is a fully typed description of Aseprite's JSON export (`frames`, `meta`, `frameTags`, `layers`, `slices`). `Fighter.init()` fetches PNG and JSON in parallel (`loadImageBitmap` in `src/helpers/getBitmapImage.ts` uses `fetch` + `createImageBitmap`), then derives animation names by stripping `-[0-9]+$` from every frame key. It never reads `meta.frameTags`.

`src/renderers/index.ts` holds the `Renderer` interface (`init`, `render`) and `RendererManager`, which asks `navigator.gpu` for an adapter/device and otherwise fails. `src/renderers/webgpu/webgpu.ts` (207 lines, the largest file) configures the canvas context, builds a bind-group layout (sampler, texture, `Dimensions` uniform), creates one render pipeline, and per frame encodes a pass that per object uploads a six-vertex quad with UVs from the current frame, writes `{ world, screen }` into a uniform, binds the cached `GPUTexture` and draws 6 vertices. The WGSL in `shaders.ts` maps world coordinates to clip space via `2 * (position.xy / dims.world) - 1`.

### Stack

- **TypeScript 5.9** strict with `noUncheckedSideEffectImports` and `erasableSyntaxOnly`.
- **Vite 7** dev server and bundler (`vite.config.js` copies `package.json` to `dist` via `rollup-plugin-copy`).
- **WebGPU** (`@webgpu/types`) the only real backend; **NW.js** (`nw` ^0.104) for the desktop window.
- No game framework, no physics library, no test runner.

## What works well

- The Aseprite contract is typed properly: `AsespriteExport.ts` documents frame rects, tag ranges, layer blend modes and slice pivots, so extending the loader means reading an interface, not guessing at JSON.
- `RendererManager` separates backend selection from rendering: `Renderer` is a two-method interface, so a second backend is a drop-in type.
- Texture upload is cached in `WebGPURenderer.textures` keyed by `image.url`, so a spritesheet is created once, not per frame.
- `Fighter.pos` converts a top-left authoring coordinate into the world's bottom-origin space (`gameWorld.height - size.h - y`), keeping asset and render coordinates apart.

## What I'd change

- **`WebGPURenderer.render` allocates two GPU buffers per object per frame and never destroys them.** In `src/renderers/webgpu/webgpu.ts`, `device.createBuffer` for the vertex data and again for the dimensions uniform sit inside the `for (const gameObject of gameObjects)` loop, with no `buffer.destroy()` and no reuse. At 60 FPS that is thousands of leaked buffers a minute; the uniform is identical for every object and belongs in `init`.
- **The WebGL fallback is a stub with its body commented out.** `src/renderers/webgl.ts` is 10 lines: `init(canvas)` and `render(dt)` are empty. In `RendererManager.init` the two fallback lines are commented out and replaced by `alert('WebGPU not supported, falling back to WebGL is currently disabled.')` then `throw`. The interface promises a fallback the code refuses to give.
- **Animation timing ignores the file.** `Fighter.init` sets `duration: 100 / 1000` for every animation, so frames play at 100 ms regardless of what `AsepriteFrame.duration` says — a field declared in `AsespriteExport.ts` and never read. Same for `meta.frameTags`, the correct way to define clips; frame names are parsed with a regex instead.
- **`Fighter` imports `gameWorld` from `main.ts`** while `main.ts` imports `Fighter` — a circular dependency that only works because `gameWorld` is evaluated first.
- **`gameWorld.depth` and `Position.z` exist but nothing uses them**: no depth sort, no z-test, so draw order is array order.

## Still outstanding

- No README — no `.md` file at all, so the two-process setup (`npm run dev` must be running for `npm start`, which loads `http://localhost:5173`) is discoverable only from `package.json`.
- No tests and no lint/format config; single commit (`8ef1e33 initial commit`).
- No input handling, no player control, no second character — `gameObjects` in `main.ts` is a one-element array.
- `designs/overview.drawio` exists but nothing in `src/` references it; the plan it describes is not yet reflected in code.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
