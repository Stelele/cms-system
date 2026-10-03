# handmade-combat-gods

A WebGPU renderer written from scratch in TypeScript — no game framework, just raw GPU buffers, a WGSL shader, and hand-rolled matrix math. It draws two colored rectangles at a fixed camera.

## What it does

Open `index.html` through Vite and a 640×320 canvas renders two translated quads (one purple, one green) in perspective. Resizing the window re-fits the canvas via CSS. If WebGPU is unavailable, a `#errors` div is filled with "Web GPU not supported". There is no interaction — no camera controls, no input; `main.ts` builds two entities and the animation loop runs.

## How it works

`src/main.ts` (18 lines) is the entry point: it constructs a `Renderer`, awaits `renderer.start()`, then builds two `PrimitiveEntity` instances with a fluent builder (`.fill([...]).rect([w, h]).translate(0, 0, 10)`) and calls `renderer.loadObjects([obj2, obj1])`.

`src/Renderer/Renderer.ts` (252 lines) owns everything GPU-side:

- `initCanvas()` requests the adapter/device, configures the `GPUCanvasContext`, and registers `resizeCanvas`.
- `initPipeline()` creates one shader module from `src/Renderer/shaders/test.shader.ts` (40 lines of WGSL) and a bind group layout with four entries: a per-instance storage buffer (color + transform), a uniform (`time` + camera view-projection matrix), a vertex storage buffer, and an index storage buffer.
- `loadData()` (called every frame in `render()`) concatenates every object's `color` and `transformation` into `propsData`, its vertices into `vertexData`, and its indices into `indexData`, uploads them, and rebuilds the bind group — recreating a buffer only when the byte size changed.
- The WGSL vertex shader indexes `points[indexes[instanceIdx][vertIdx]]` and applies `viewProjMat * transformation * point`, so a single `pass.draw(6, this.objects.length)` instanced call renders every object.
- `startAnimation(120)` gates rendering with a `Date`-based frame limiter inside `requestAnimationFrame`.

`src/Math/Mat4.ts` (298 lines) implements `perspectiveMat`, `lookAtMat`, translation/rotation/scale, and `matMultiply`; `Vec3.ts` (42 lines) is a minimal vector helper. `CameraEntity.ts` (37 lines) exposes a `viewProjMat` getter that recomputes projection × view on every access, reading canvas aspect from `document.getElementById("webgpu")`.

### Stack

TypeScript 5.6, Vite 5, and `@webgpu/types` — those are the only dependencies in `package.json`; there are no runtime libraries at all.

## What works well

- Zero runtime dependencies: the math, camera, and renderer are all in-repo (`Mat4.ts` is a complete, readable 298-line implementation).
- The instancing design is genuinely data-oriented — all instance colors/transforms in one storage buffer, one draw call per frame.
- Buffer reuse keyed on `byteLength` avoids per-frame allocation churn, and the entity builder in `PrimitiveEntity.ts` gives a clean fluent API (`fill`/`rect`/`translate`/`rotation`/`scale`).

## What I'd change

- **No depth buffer.** `initRenderPassDescriptor()` declares only `colorAttachments` (with a `// @ts-ignore` covering the missing `view` field) — no `depthStencilAttachment` — yet the camera uses `Mat4.perspectiveMat`. With a perspective camera and no depth testing, draw order is whatever the index order says; overlapping geometry will pop. `loadObjects()` also replaces the whole object list rather than appending, so there's no way to add the third entity the pipeline could already handle.
- **`loadData()` and bind-group creation run every frame** even when nothing changed, and `startAnimation(120)`'s `Date`-diff limiter inside `rAF` just drops frames on >120 Hz displays instead of using a fixed timestep or `navigator.gpu` frame pacing.
- `CameraEntity.viewProjMat` recomputes perspective + lookAt + multiply each access and reaches into the DOM for aspect ratio — the math layer shouldn't know about element IDs. `fov = Math.atan2(1, zNear)` with `zNear = 10` is an unusual FOV definition (≈5.7°).
- Error paths do `throw new Error()` with no message (`Renderer.initCanvas`), and the WGSL hardcodes `array<array<u32,6>>` indices — `rect()` is the only shape the renderer can ever draw.
- `index.html` still has the Vite template `<title>Vite + TS</title>`.

## Still outstanding

- No README and no tests — the only file matching `*test*` is `Renderer/shaders/test.shader.ts`.
- Single commit, message "got camera working"; no depth pass, no input handling, no camera controls, no geometry other than the unit quad.
- `resizeCanvas()` only sets CSS size; the 640×320 backing store never follows devicePixelRatio.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
