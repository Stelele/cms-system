# shader-land

A ShaderToy-style playground: Vue 3 frontend with Monaco WGSL editor and WebGPU fragment-shader renderer, plus a Go/SQLite API storing shaders and authenticating writes with Clerk.

## What it does

`/new` opens a playground — Monaco right, live WebGPU canvas left — where each keystroke recompiles the fragment shader, with play/pause, step-back and fullscreen below. Submitting name + description saves and redirects to `/view/:url`, reloading the shader and showing an editable form to its owner; `/` is a home grid. Browsing, viewing and creating work; editing is half-wired.

## How it works

**Frontend** (Vue 3 + Vite):

- `src/main.ts` requires `VITE_CLERK_PUBLISHABLE_KEY` and installs `vue-router` (`/`, `/view/:id`, `/new`) + `clerkPlugin`.
- `src/components/Renderer/Render.ts` (308 lines) is the core: `init()` grabs a shared `static` `navigator.gpu` device, configures the context and creates five uniforms plus a bind group; `loadFragmentShader(code)` concatenates the `Uniforms` WGSL from `Start.shader.ts` with user code and rebuilds the pipeline over a fixed full-screen triangle; `recordVideo()` wraps `canvas.captureStream()` in `MediaRecorder`.
- `Editor.vue` loads Monaco (`wgsl`), emitting `onValueChange` per edit, which `ShaderPlayground.vue` forwards to `loadFragmentShader`; `ShaderService.ts` uses raw `Axios` (`baseURL = import.meta.env.VITE_BACKEND_SVC_URL`) and adds a Clerk token only on `postShader`.

**Backend** (Go 1.23):

- `server.go` (28 lines): `godotenv.Read` → `clerk.SetKey` → `db.InitDb()` → `mux` → `:8080`.
- `routes/routes.go` registers only `initShadersRoutes`; `shaders.go` (164 lines) has `GET /shaders{,?name=,/{url}}` and `POST`/`PUT`/`DELETE /shaders/{id}`; `helpers.go` sets permissive CORS and `verifySession()` (`jwt.Verify` on `Authorization: Bearer`).
- `db/sqlite_repository.go` opens `sqlite.db` and migrates; `db/models/shader.go` (364 lines) is the data layer — inline `CREATE TABLE`, hand-written SQL, UUID → base64 → `PathEscape` `url`, `ErrDuplicate`/`ErrNotExists` sentinels.

### Stack

Vue 3, vue-router, `@clerk/vue`, Monaco, Tailwind 3 + daisyUI, `oh-vue-icons`, `@webgpu/types`; Go with `gorilla/mux`, `mattn/go-sqlite3`, `google/uuid`, `clerk-sdk-go/v2`, `godotenv`.

## What works well

- The renderer is a clean, reusable class: shader source is data (`Start.shader.ts` template literals), rebuild is a one-call `loadFragmentShader`, and the uniform set is declared once in WGSL and once in `setupBuffers()`.
- Reuse-by-default: the `GPUDevice` is `static`, so four home-page canvases share one device; `Renderer.vue` retries `loadFragmentShader` until `isReady`.
- The repository returns typed sentinels (`ErrNotExists`, `ErrDuplicate`, `ErrUpdateFailed`) mapped by handlers to 404/400 — `GET /shaders/{url}` is genuinely correct about it.
- Writes go through `user.Get(ctx, claims.Subject)`, so stored `userId`/`userName` come from Clerk, not the body.

## What I'd change

- **`handleShadersPost` (shaders.go:64-72) is missing a `return`.** On `user.Get` failure it writes 403 and then falls through to `req.UserId = user.ID` — `user` is nil, so a bad token panics the handler instead of returning 403.
- **`handleShaderUpdate` and `handleShaderDelete` never call `verifySession`.** Anyone can `PUT`/`DELETE /shaders/{id}` with no header at all. Both also omit `return` after their `w.WriteHeader(400)` blocks, so a bad body yields two `WriteHeader` calls.
- **`GetByUrl` scans `&shader.UserId` twice** (shader.go:265-266) where the second column is `userName`. `UserName` is never populated and `UserId` ends up holding the user name — which is exactly the field `ViewPage.vue` compares against `user.value.id` for `isEditable`, so the edit form can never appear.
- **`GetById` returns `err` instead of `err2`** when `rows.Scan` fails (shader.go:211-213), reporting success with a nil `Shader`; its `SELECT` also omits `userName`.
- **`Migrate()` declares a `FOREIGN KEY … REFERENCES users(id)` on a `users` table that is never created**, and `collection.http` still carries `POST /users`, `GET /users`, `POST /users/password?password=…` requests for routes that `routes.go` does not register — leftovers from an abandoned password scheme replaced by Clerk.
- **`ShaderPlayground.vue` mutates the DOM by `document.getElementById('time'/'fps'/'resolution')`** instead of refs, and the ids `animCheck`/`recordCheck` are duplicated by every `ShaderDisplay` on the home page, so the checkboxes no longer correspond 1:1 to their renderer.
- `Render.ts` wraps the recording blob as `{ type: "video/mp4" }` while `MediaRecorder` is constructed with no `mimeType`, so browsers hand back WebM bytes labelled MP4.

## Still outstanding

- **Zero tests.** No `*_test.go` in `backend/`, and `frontend/package.json` defines only `dev`/`build`/`preview`.
- **`HomePage.vue` fetches shaders and never renders them**: `shaders.value = await ShaderService.getShaders()` is assigned, but the template hard-codes four `<ShaderDisplay :shader-code="StartShaderFs" />` tiles and a static skeleton. The home page is a mock-up.
- **The update flow is dead**: `ViewShaderDetails.vue` emits `onUpdate`, but `ViewPage.vue` never binds it, and there is no delete UI despite the backend route existing.
- `TopAppBar.vue`'s search input has no `v-model`/handler, and its "Browse" link is hardcoded to `RouterLink to="/view/1"`.
- `.env` is committed with `{{CLERK_SECRET_KEY}}` / `{{VITE_BACKEND_SVC_URL}}` placeholders (no real secrets), but `backend/.gitignore` ignores `**/.env.*` and not `.env`, and an unset `VITE_BACKEND_SVC_URL` makes every request go to the literal base URL `{{BACKEND_SVC_URL}}`.
- No root README — `frontend/README.md` is still the Vite template text. `server.go` also `log.Fatal`s whenever `.env.local` is absent, which it is in a fresh clone (`godotenv.Read` returns on the first missing file).

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
