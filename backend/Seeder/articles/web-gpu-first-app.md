# web-gpu-first-app

A Conway's Game of Life on the GPU: a compute shader steps a 32×32 grid every 500 ms, drawn as instanced quads.

## What it does

`npm start` serves `http://localhost:1234`: a black page of randomly seeded coloured cells flipping alive/dead twice a second. Cells wrap at the edges (`%`), dead cells shrink to zero rather than vanish, and colour comes from grid position (`vec4f(c, 1 - c.x, 1)`). No pause, step, reseed or cell-size control.

## How it works

`src/index.ts` (240 lines) is one `async function init()`:

1. `resize()`, then the WebGPU handshake: `navigator.gpu` guard, adapter, device, `canvas.getContext('webgpu')`, `context.configure`.
2. A uniform buffer plus two `STORAGE | COPY_DST` ping-pong buffers (`cellStateStorage[0]`/`[1]`), seeded `Math.random() > 0.4` at `i += 3` steps.
3. A six-float vertex buffer (two triangles, 8-byte stride), plus `CellShader`/`ComputeShader` modules.
4. One `bindGroupLayout` (uniform, `read-only-storage`, `storage`) and **two bind groups swapping those storage buffers**: A `[state0, state1]`, B `[state1, state0]`.
5. `createPipelineLayout` shared by `cellPipeline` and `simulatePipeline`, not `layout: 'auto'`.
6. `setInterval(updateGrid, 500)`: each tick encodes a compute pass (`dispatchWorkgroups(4, 4)`) then a render pass, alternating `bindGroups[step % 2]` and calling `draw(6, GRID_SIZE * GRID_SIZE)`.

The WGSL sits in two files: `src/ComputeShader.ts` exports `WORKGROUP_SIZE = 8`, interpolated into `@workgroup_size(...)` so TS and shader cannot drift; `computeMain` counts eight neighbours and applies the 2/3 rule, `cellIndex()` wrapping modulo. `src/CellShader.ts` positions instances from `@builtin(instance_index)` and `grid`.

### Stack

- **TypeScript 5.4 + `@webgpu/types`**; **webpack 5 + ts-loader** with `CopyPlugin`/`HtmlWebpackPlugin` — the same scaffolding as `learn-pixi-js`.
- **WebGPU / WGSL** only — no runtime dependency, just devDependencies.

## What works well

- **Double-buffering is correct**: compute writes binding 2 via `bindGroups[step % 2]`, then the render pass reads the fresh buffer via `bindGroups[step % 2]` — ping-pong done right.
- **The bind group layout is declared once** and fed to `createPipelineLayout`, shared by both pipelines, without duplicated flag declarations.
- **`WORKGROUP_SIZE` is a single source of truth**, used in the WGSL string and in `Math.ceil(GRID_SIZE / WORKGROUP_SIZE)` for the dispatch count.
- Every GPU object has a descriptive `label` (`"Cell State A"`, `"Game of Life simulation shader"`), keeping Chrome's WebGPU inspector readable.
- The neighbour count is eight explicit `cellActive()` calls instead of a nested loop, so `cellIndex()` is the only place offsets can go wrong.

## What I'd change

- **`resize()` never sets `canvas.width` or `canvas.height`** — it only writes `canvas.style.width/height/margin*`. The drawing buffer is whatever `index.ejs` declared (and that markup writes `width="620px"`, which is not a valid HTML integer attribute), while CSS stretches the element. The GPU is rendering into a fixed, stale-resolution target on every window resize.
- **`init()` throws into an unawaited promise.** It is called as bare `init()` at the bottom of the file, and its first three statements are `throw new Error("WebGPU not supported on this browser")`, `"No appropriate GPUAdapter found."`, `"Could not get canvas context"`. On an unsupported browser the page is a black rectangle with an unhandled rejection in the console and no message for the user.
- **No GPU error handling at all**: no `device.addEventListener('uncapturederror', …)`, no `pushErrorScope`/`getCompilationInfo` around either `createShaderModule`. A WGSL typo in `ComputeShader.ts` silently stops the animation.
- **Everything lives in one 200-line `init()` closure** — buffers, layout, pipelines, the tick loop, `updateGrid`. There is no module boundary to test or reuse, and `setInterval(updateGrid, UPDATE_INTERVAL)` is created inside it with no handle stored, so the interval can never be cleared (no pause, no teardown on page hide).
- **The seed loop is `for (let i = 0; i < cellStateArray.length; i += 3)`**, which touches only a third of the 1024 cells (an even mix of `i % 3` classes) rather than every cell — almost certainly not the intent, and it leaves `Uint32Array` entries beyond the last hit at whatever `Math.random()` set.

## Still outstanding

- No tests, no `test` script in `package.json`, no lint or format config, no CI.
- No README — the repo contains `src/`, `webpack.config.js`, `tsconfig.json`, `package.json` and a `LICENSE` (MIT, 2024), and nothing explaining how to run it or what it demonstrates.
- `package.json` has `"license": ""` despite the MIT `LICENSE` file, and no `"name"`.
- The controls a Game of Life demo normally gets — pause, step-once, clear, reseed, speed — do not exist; `UPDATE_INTERVAL = 500` and `GRID_SIZE = 32` are constants with no way to change them without editing `index.ts`.
- `src/index.ejs` still carries a `#pixi-content` CSS rule from the template it was copied from, which matches no element in the page.
- Nothing is marked `TODO`/`FIXME`/`HACK` anywhere in the source.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
