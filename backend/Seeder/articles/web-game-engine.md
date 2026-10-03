# web-game-engine

A from-scratch WebGPU game engine in TypeScript — resource loading, bitmap fonts, cameras, viewports and a typed renderable hierarchy — proven out by one demo scene.

## What it does

`npm run dev` shows three viewports: a large centre view (background, hero, portal, two minions, helper text), a bottom-left inset locked to the portal, a bottom-right inset locked to the left minion — each with its own camera. Keyboard input in `Scene1.update` switches the main camera's target (<L>/<R> minions, <P> portal, <H> hero), zooms (<N>/<M>/<J>/<K>) and shakes on <Q>. One scene only: no game loop with entities, collision or audio.

## How it works

`src/main.ts` calls `main()` in `src/MyGame/index.ts`: `gEngine.setWorldDimensions(640)`, `initializeWebGPU("GLCanvas")`, `clearCanvas`, `Resource.loadResourcesFromManifest(ResourceManifest)`, then `new Scene1()` and `gEngine.GL.setScene(scene1)`.

`src/Engine/Core/EngineCore.ts` is a static facade: `gEngine.GL` (Renderer), `gEngine.Input` (a `Record<string, boolean>` filled by keydown/keyup), `gEngine.Resource` (ResourceManager). Init also loads `/Fonts/system_default_font` and compiles `primitive`, `texture`, `animated-texture` programs (the last two reuse `TextureVS`/`TextureFS`).

`src/Engine/Core/Rendering/Renderer.ts` (398 lines, the largest file) is the heart: `init()` acquires adapter/device, configures the canvas, creates eight bit-indexed samplers and starts a frame-limited loop. `setScene(scene)` asks for `getRenderingPassesInfo()` — `{ camera, renderables, viewPort }` — and materialises each renderable into an `IObjectInfo` (buffers, uniforms, optional texture + UVs) once. Each frame `render()` sets viewport/scissor per pass, writes the camera view-projection, then per object colour, vertices, transform, bind, draw.

Render types go through `getRenderTemplate(type)` in `Templates/RenderTemplate.ts` — `PrimitiveRenderTemplate`, `TextureRenderTemplate`, `AnimatedTextureRenderTemplate`, each with `getDrawObject`, `processRenderPass`, `configureBindGroupLayout`, `configurePipelineBuffers` and `getBindGroup`; buffer helpers live in `Templates/helpers.ts`. Renderables mirror this: `Renderable` (218 lines) plus `BoundingBox`, `TextureRenderable`, `AnimatedTextureRenderable`, `TextRenderable`.

Resources go `ResourceManager` → `FontLoader` (202 lines, parses `.fnt` XML), `ImageLoader`, `AudioLoader`, `TextFileLoader`, over a `ResourceMap` tracking `refCount`/`outstandingLoadsNum`. `MyGame/ResourceManifest.ts` declares five fonts and two images.

### Stack

- **TypeScript 5.2** strict with `@webgpu/types`; raw **WebGPU** (`navigator.gpu`, WGSL strings in `src/Engine/Shaders/`) — no three.js, no Pixi.
- **Vite 5** dev/build; **vitest 2** installed but unused.
- Math (`Mat4` 343 lines, `Mat3`, `Vec2`, `Vec3`, `Interpolate`, `ShakePosition`) is hand-written.

## What works well

- The render-template strategy is the right shape: a new renderable type means one template plus a `case` in `getRenderTemplate`, not a new branch in `Renderer`.
- Multi-pass rendering with per-pass camera and viewport/scissor works: `Scene1.getRenderingPassesInfo` returns three passes with different cameras and inset viewports.
- Camera work is self-contained — `Camera.ts` (160 lines) has `panWith`, `clampAtBoundary`, `zoomBy`, `zoomTowards`, `shake`, with `CameraState`/`CameraShake`/`Interpolate` smoothing.
- `clearCanvas` and `getFillColorInfo` validate length and 0–1 range and throw rather than sending bad data to the GPU.
- Strict TS plus `noUnusedLocals` across ~3,500 lines of engine code with no `any` escapes in the hot path.

## What I'd change

- **`setScene`'s cleanup calls `.unmap()` on buffers that were never mapped** — `Renderer.ts:246`, `:251-254`, `:259`. Only `mapAsync`/`getMappedRange` buffers may be unmapped; the WebGPU spec throws an `OperationError` otherwise. The first `setScene` escapes it (the `renderingInfos` array is empty), so switching scenes a second time would fail. It also destroys `objectInfo.texture`, which came from the shared `ResourceManager`, not from the renderer.
- **`clearCanvas` has no effect.** `MyGame/index.ts` calls `gEngine.GL.clearCanvas([0.9, 0.9, 0.9, 1])`, and `clearCanvas` writes `clearValue`, but `configureRenderPassDescriptor` sets `loadOp: "load"` (`Renderer.ts:217`) — the attachment is never cleared.
- **The frame limiter renders at half rate.** `startAnimation` sets `start = undefined` after each render, so the next frame's `elapsed` is 0 and the one after that crosses the threshold — at 60 Hz you get ~30 renders/sec.
- **Per-frame allocation in the hot loop.** `getBindGroup` (`Renderer.ts:193`) creates a fresh `GPUBindGroup` for every object every frame, and `getRenderTemplate` news up a template object on every call in both `getBindGroup` and `processRenderPass`.
- **`renderingInfos` is a snapshot.** `setScene` computes it once; anything a scene adds later never renders. `Scene1` only works because its async `init()` happens to run synchronously — the moment it awaits anything, `getRenderingPassesInfo()` returns `[]` while the snapshot is taken and the scene stays blank forever.

## Still outstanding

- **No tests despite the setup**: `vitest@^2.0.3` is installed and typed in `tsconfig.json`, yet there is no `test` script and no `*.test.ts`/`*.spec.ts` file in the repo. `Mat4.ts` (343 lines of matrix math) and `Camera` would be the obvious first targets.
- **Audio and text resources are implemented but unused**: `public/Sounds/` holds three clips and `ResourceManager.loadResource` handles `type === 'sound'`, but `ResourceManifest.ts` declares no sound entry; `TextFileLoader` has no caller either.
- No README — the repo only has an MIT `LICENSE`.
- `index.html` sets `body { height: 99.74% }`, a magic number compensating for canvas layout rather than a computed size.
- Single commit (`87a32f5 refactored code to extract functions linked to different renderable types…`), no TODO markers in the source.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
