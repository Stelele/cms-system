# blog-webgpu-hello-world

A four-file, no-build WebGPU example that draws one RGB triangle on a full-viewport canvas. Written as companion code for a blog post.

## What it does

Open `index.html` in a browser and you get a black, full-window page with a grey clear colour and a single triangle whose three corners are red, green and blue. Nothing animates and nothing is interactive — `render()` is called exactly once. If `navigator.gpu` is unavailable, an `alert()` says "need a browser that supports WebGPU".

## How it works

There is no bundler, no `package.json`, no dependencies. The repo is `index.html` (35 lines), `main.js` (106 lines), `README.md` (4 lines) and a `LICENSE`.

`index.html` loads `<script src="main.js" defer>` and defines a flex `.container` that centres a `<canvas id="webgpu-output" class="display">`, where `.display` is `min-width: 100%; min-height: 100%` on a black body.

`main.js` is a single `async function main()`:

1. `navigator.gpu?.requestAdapter()` → `adapter?.requestDevice()`, guarded by `if (!device) fail(...)`.
2. `canvas.getContext('webgpu')`, `navigator.gpu.getPreferredCanvasFormat()`, `context.configure({ device, format })`.
3. `device.createShaderModule({ code: ... })` with the WGSL written inline as a template literal: a `VertexOut` struct, a `vs()` that indexes a hard-coded `array(vec2f, 3)` of positions and a matching array of colours, and an `fs()` that returns `in.color`.
4. `device.createRenderPipeline({ layout: 'auto', vertex: { module }, fragment: { module, targets: [{ format }] } })`.
5. A `renderPassDescriptor` object literal with `clearValue: [0.3, 0.3, 0.3, 1]`, `loadOp: 'clear'`, `storeOp: 'store'` and a `view` slot left commented as "to be filled out when we render".
6. `render()` fills that `view` from `context.getCurrentTexture().createView()`, then `createCommandEncoder` → `beginRenderPass` → `setPipeline` → `draw(3)` → `end` → `queue.submit`.

### Stack

- **Plain JavaScript (ES modules not used; a single deferred script)** — no transpilation, no type checking.
- **WebGPU / WGSL** — the only API in use; the shader source lives inside a JS template literal.
- **Raw HTML + CSS** — layout and canvas sizing are done with a flex container, not JS.

## What works well

- It is genuinely readable end to end: the whole WebGPU lifecycle (adapter → device → context → module → pipeline → pass → submit) is in one function, in the order the API expects.
- Failure to get a device is handled (`navigator.gpu?.` optional chaining plus an explicit `fail()`), so opening it in Firefox at the time didn't throw a raw `TypeError`.
- Every GPU object carries a `label` — `'our hardcoded red triangle shaders'`, `'our basic canvas renderPass'`, `'our encoder'` — which makes `chrome://gpu` debugging output legible.
- The `renderPassDescriptor` is defined once and only its `colorAttachments[0].view` is mutated per frame, which is the shape you'd want if a `requestAnimationFrame` loop were added later.

## What I'd change

- **The drawing buffer size is never set.** `main.js` never assigns `canvas.width` or `canvas.height`, and there is no `resize` listener, so the buffer stays at the canvas default while the `.display` CSS rule stretches the element across the window — the triangle is an upscaled 300×150-ish render on any real screen.
- **The WGSL is never validated.** There is no `device.pushErrorScope('validation')` / `getCompilationInfo()` and no `device.addEventListener('uncapturederror', ...)`. A typo in the inline shader fails silently as a blank canvas.
- **Every vertex colour has `alpha = 0`** (`vec4f(1.0, .0, .0, .0)` and friends) while the context uses WebGPU's default `alphaMode: "premultiplied"`, so the output does not composite as the pure RGB the arrays are labelled with.
- **The pipeline label lies**: `'our hardcoded red triangle pipeline'` for a shader that emits red, green and blue vertices.
- The README says "Just click on open index.html in the browser and bam!!! it works" with no note about needing a WebGPU-capable browser or a secure context.

## Still outstanding

- No tests, no linter, no formatter, no CI — and no `package.json`, so there is nowhere for any of them to hang.
- The code renders exactly one frame; there is no animation loop, no interaction, no cleanup/`device.destroy()`.
- Nothing in the source is marked `TODO`/`FIXME`; the only trace of unfinished work is the comment in `renderPassDescriptor` — `// view: <- to be filled out when we render`.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
