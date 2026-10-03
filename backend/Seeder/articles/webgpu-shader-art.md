# webgpu-shader-art

A browser playground of WGSL fragment shaders rendered as a 1024×1024 instanced grid on WebGPU — shadertoy-style pieces ("katas"), smiley faces, palettes, patterns — kept animating by a per-frame time uniform.

## What it does

You run `npm start` (webpack dev server on port 1234), a page opens with one full-window canvas, and a single shader animates forever. Twelve fragment shaders exist in `src/shaders/fragments/` (FirstArt, SmileyV1–V3, WaveyRect, SecondArt, OneD/Color/Shape/Matrix/Patterns katas, Test), each assembled from shared WGSL helper strings. There is no UI: which one you see is decided by an array index in the source.

## How it works

Entry point is `src/index.ts` (3 lines): it grabs `#webgpu-canvas` and calls `App.start(canvas)`. `App` in `src/Start.ts` (292 lines) is a static class that runs `setupCanvas → setupBuffers → setupShaderModule → setupBindGroupLayout → setupPipeline → setupBindGroups → setupRenderPassDescriptor → startAnimation(60)`.

Four GPU buffers feed the bind group: resolution (vec2f), timeStep (u32 milliseconds), grid (vec2f, 1024) and a two-triangle vertex buffer. `src/shaders/vertices/GridArrayVertexShader.ts` lays out the three uniform bindings and computes each cell's position from `@builtin(instance_index)`; `render()` then calls `pass.draw(3, 1024*1024)` — 1,048,576 instances per frame. Fragment shaders interpolate `Uniforms`, `VertexOut` and helper functions from `src/shaders/functions/` (`PaletteFunc`, `CircleFunc`, `RectFunc`, `MapFunc` at 263 lines with easing enums, `TransformationFunc`, `PolygonFunc`, `ColorWheelFunc`, `RemapFunc`) as template literals, so composition happens at TypeScript level before WGSL is handed to `createShaderModule`.

The animation is a `requestAnimationFrame` loop throttled to 60 fps in `animate()` (Start.ts:257), which rewrites the resolution and time uniforms each frame and re-fetches `getCurrentTexture().createView()` because the old view is stored in the pass descriptor. `App.device.lost.then(...)` (Start.ts:54) cancels the frame and recursively calls `App.start(canvas)` to rebuild everything after a device loss.

### Stack

TypeScript 5.4.5 compiled by ts-loader under webpack 5, `@webgpu/types` for typings, html-webpack-plugin from `src/index.ejs`, copy-webpack-plugin for `static/`. Zero runtime dependencies — every dependency in `package.json` is a devDependency.

## What works well

- Shader sharing is real: `fragments/index.ts` and `functions/index.ts` re-export small WGSL fragments, and each art file imports only what it uses.
- Device-loss recovery is handled deliberately rather than crashing the tab.
- The uniform/buffer layout is explicit (`bindGroupLayout` with three `GPUShaderStage.VERTEX | FRAGMENT` bindings) instead of `layout: "auto"`, so the pipeline contract is readable.
- `webpack.config.js` disables console output in production via Terser `drop_console` and maps sourcemaps only in development.

## What I'd change

- **The shader is hardcoded**: `Start.ts:164` sets `code: App.fragmentShaders[11]`, i.e. `TestFragmentShader`. Eleven other exported shaders are unreachable without editing and rebuilding — no query param, no picker.
- `getDevice()` (Start.ts:76–85) does `const device = adapater?.requestDevice()`, a Promise, then `if (!device)` — that branch is dead code, and there is no null-adapter message before it throws.
- `resize2` (helpers/ResizeWindow.ts) sets `canvas.width = window.innerWidth` with no `devicePixelRatio`, so the canvas is blurry on HiDPI displays; the same file still carries an unused CSS-letterboxing `resize()` (640×320) left from a game template, as does the `#pixi-content` rule in `src/index.ejs`.
- `timeStep` is a `Uint32Array` of milliseconds (Start.ts:121, 277) — it wraps after ~49.7 days of continuous rendering.

## Still outstanding

- No README, no tests, no `test` script, no CI directory, and a single git commit ("added tree").
- No fallback UI when `navigator.gpu` is missing — the thrown error surfaces only as an unhandled rejection.
- No shader-selection mechanism, so the repo reads as a library of work-in-progress pieces with one visible at a time.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
