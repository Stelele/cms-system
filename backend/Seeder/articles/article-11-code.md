# article-11-code

Sample code for a blog article: a minimal WebGPU app drawing one fullscreen triangle and animating a fragment shader with a time uniform.

## What it does

`npm install && npm run dev` opens a black page with a canvas sized to the window. The fragment shader draws concentric rings centred on screen: it converts framebuffer position into an aspect-corrected coordinate, takes the distance from the origin, feeds it through `sin(d * 30 + t * 4)` and thresholds with `smoothstep(0.7, 0.71, d)`, giving hard-edged rings that pulse outward as `t` grows. Resizing keeps the pattern centred and proportioned.

No controls, no uniforms exposed to the page, no other shaders — one draw call, three vertices, one effect.

## How it works

`src/main.ts` (10 lines) registers a `resize` listener, calls it once, and constructs `Renderer` from `src/Renderer.ts` (167 lines). `Renderer.init()` runs five steps:

1. **`setupDevice`** — gets canvas `#webgpu`, requests the `navigator.gpu` adapter and device, picks `getPreferredCanvasFormat()`, configures the `GPUCanvasContext`. Missing adapter or context throws `"No device"` / `"No context"`.
2. **`initPipeline`** — creates one `GPUShaderModule` from `TestShader`, one bind-group layout with a uniform buffer visible to both stages, and a pipeline with **no vertex buffers**: the vertex stage reads `@builtin(vertex_index)` and indexes a hard-coded `array<vec2f, 3>` of oversized coordinates `(-1,3), (-1,-1), (3,-1)`.
3. **`initBuffers`** — allocates a 3×`f32` uniform buffer (`Props { width, height, time }`) and the bind group referencing it.
4. **`initRenderPassDescriptor`** — a color attachment with `loadOp: "clear"` and a black `clearValue`, but no `view`.
5. **`startAnimation(60)`** — a `requestAnimationFrame` loop that, once `1000/60` ms have passed since the last render, advances a local `time` by `targetMs / 1000` and calls `render(time)`.

`render` fetches `context.getCurrentTexture().createView()`, assigns it to the color attachment, writes `[canvas.width, canvas.height, time]` into the uniform, sets pipeline and bind group, and calls `pass.draw(3)`.

`src/helpers/resize.ts` sets `canvas.width`/`canvas.height` from `window.innerWidth`/`innerHeight`; because `Props.width/height` are rewritten from the canvas each frame, aspect correction follows automatically.

### Stack

- **TypeScript 5.6** strict, with `@webgpu/types` for the `GPU*` typings.
- **Vite 5.4** dev server and bundler; build runs `tsc && vite build`.
- **WebGPU + WGSL**, hand-written in `src/shaders/test.shader.ts` as a template literal — no rendering library, package named `shader-toy-gpu`.

## What works well

- Small enough to read in one sitting: 218 lines of TypeScript across five files, shader 34 lines.
- Drawing with `vertex_index` instead of a vertex buffer is right for a fullscreen pass — it removes buffer, vertex layout and stride from the pipeline entirely.
- The `Props` uniform is the complete CPU/GPU interface: three floats written once per frame — exactly the shape a shader-toy wants.
- Aspect ratio is handled in the shader (`uv.x *= props.width / props.height`) rather than by letterboxing, so the pattern stays circular at any window size.

## What I'd change

- **`// @ts-ignore` suppresses a real type error.** In `initRenderPassDescriptor`, the color attachment is declared without `view`, which is why the cast is needed; `render()` fills it in later. Making the descriptor a partial and assigning `view` before `beginRenderPass` (or typing it as `GPURenderPassDescriptor` with a late-bound view) would remove the ignore and keep type safety on the rest of the object.
- **Frame timing drifts away from wall clock.** `startAnimation` uses `new Date()` rather than the `timeStep` argument `requestAnimationFrame` already provides, and advances `time` by a fixed `targetMs` per rendered frame. If the browser drops to 30 FPS the animation runs at half speed, and after a backgrounded tab returns, `diff` is huge but only one `targetMs` is credited — `time` permanently lags real elapsed time.
- **`renderer.init()` is never awaited.** `start()` in `src/main.ts` is synchronous and calls an async method without `await`, so a `"No device"` throw surfaces as an unhandled promise rejection with no on-screen message.
- **`resize` ignores `devicePixelRatio`**, so on a HiDPI display the canvas is rendered at CSS resolution and upscaled — visibly soft for a shader demo where line quality is the point.
- **`index.html` still has `<title>Web Game Engine</title>`**, carried over from the engine project this was forked from.

## Still outstanding

- No tests, no linter, no formatter: `package.json` has only `dev`, `build` and `preview`.
- The README says this supports "my blog article" but does not link to it or name which article.
- The shader is called `test.shader.ts`, and there is no second shader to compare against — the file name suggests experiments that never landed.
- Single commit (`19b1e53 initial commit`); no TODO markers in the source.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
