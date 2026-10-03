# webgpu-template

A minimal WebGPU starter for TypeScript: one canvas, one grid shader, one draw call — the seed project the shader-art playground grew out of.

## What it does

Run `npm start` and a webpack dev server on port 1234 opens a page with a full-window canvas rendering a 1024×1024 grid of cells, each coloured by its own position (`cell / grid` for red, `1 - c.y` for blue). It renders a single static frame and stops. There is no animation and no UI beyond the canvas.

## How it works

`src/index.ts` (5 lines) calls `App.init()`. `App` in `src/app.ts` (161 lines) is a static class whose `init()` runs five named steps: `setupScreenResizing → setupGPUDeviceAndCanvasContext → setupBuffers → setRenderPipeline → setBindGroup → renderLoop`.

Device acquisition (app.ts:44–75) checks `navigator.gpu`, then `requestAdapter()`, then `requestDevice()`, throwing a distinct error at each failure, and configures the canvas context with `navigator.gpu.getPreferredCanvasFormat()`. Two buffers are created: a vertex buffer holding two triangles (six float2s covering clip space) and an 8-byte uniform buffer holding `GRID_SIZE = 1024`. `setRenderPipeline` builds a `GPURenderPipeline` with `layout: "auto"` from the single WGSL module `CellShader`, entry points `vertexMain`/`fragmentMain`. The vertex shader maps `@builtin(instance_index)` to a grid cell offset, so `pass.draw(3, GRID_SIZE * GRID_SIZE)` draws 1,048,576 instances.

`src/shaders/CellShader.ts` (31 lines) is the whole shader source, a template literal with a `VertextInput`/`VertextOutput` struct pair (typos included) and a `@group(0) @binding(0)` uniform grid size.

### Stack

TypeScript 5.4.5 with `@webgpu/types`, bundled by webpack 5 via ts-loader; html-webpack-plugin renders `src/index.ejs`, copy-webpack-plugin copies `static/`; rimraf and npm-run-all wire the `build`/`clean` scripts. All dependencies are devDependencies — no runtime packages.

## What works well

- `package.json` and `webpack.config.js` are byte-identical to webgpu-shader-art's, so the shader repo needed no build-system changes to grow.
- The GPU setup is split into single-purpose static methods with explicit error messages ("No adpter is available at the moment"), making the WebGPU bootstrap sequence easy to read end to end.
- `setupScreenResizing` registers a `resize` listener before the first draw, so layout intent is clear even though the implementation is incomplete.

## What I'd change

- **The frame is drawn once.** `renderLoop()` (app.ts:147–161) encodes and submits exactly one render pass; `grep -rn requestAnimationFrame src` returns nothing. The name promises a loop that does not exist — the grid never updates.
- **`resize()` never resizes the drawing buffer.** `src/helpers/ResizeWindow.ts` (23 lines) only sets `canvas.style.width/height/margins` around a hardcoded 600×600 base; `canvas.width`/`canvas.height` are never assigned anywhere in `src/`, so the backing store stays at the HTML default 300×150 and is stretched by CSS.
- The pipeline draws over a million instances per pass with no early-out or frame pacing, and `clearValue: [1, 1, 1, 1]` is set but invisible because the full-screen grid overwrites it.
- `src/index.ejs` still carries a `#pixi-content` CSS rule inherited from a PixiJS template, and there is no fallback message when `navigator.gpu` is absent — the thrown error surfaces only as an unhandled promise rejection.

## Still outstanding

- No README, no `test` script, no test directory, no `.github/` workflows, one commit ("made basic setup").
- No animation loop, no real resize handling, no error UI for unsupported browsers.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
