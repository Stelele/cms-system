# combat-gods

A 2D fighting-game prototype in the browser — PixiJS 8 and TypeScript — with a single stickman fighter standing on a ground platform.

## What it does

Boots a fixed 1280×640 canvas (background `#1099bb`) letterboxed and scaled to fit any window. `A`/`D` walk the fighter left and right, `Space` jumps; `W`/`S` are mapped to `UP`/`DOWN` by the keymap but nothing reads them. The stickman plays an idle or walk `AnimatedSprite`, flips to face your input, and a red `Platform` strip at the bottom stops it via AABB collision. That's the whole game — no opponent, no attacks, no hit detection.

## How it works

`index.html` loads `src/main.ts`, whose top-level `init()` creates the Pixi `Application` (`preference: "webgl"`, 1280×640), runs `axios.get('/assets/manifest.json')` → `Assets.init({ manifest, basePath: "assets" })` and `Assets.backgroundLoadBundle(...)` for every bundle, calls `KeyboardInputHandler.initialize()`, registers `resize()` for letterboxing, then `StateMachine.setState(new TestState())` and `app.ticker.add(StateMachine.update)`.

`src/StateMachine/index.ts` holds one async `IState` (`enter`/`leave` return `Promise<void>`). `TestState` owns an `IScene[]` array; `src/Scenes/TestScene.ts` constructs a `Stickman` and a `Platform`, awaits `stickman.initialize()`, then each frame calls `stickman.update(dt)` followed by `Physics.isCollision(stickman, platform, dt)`.

`src/Sprites/Stickman/Stickman.ts` (216 lines) is the bulk of the game: it implements `INode` and `IPhysicsObj`, loads `Assets.loadBundle(curSkin)`, fetches `/stickman-animations.json` (946 KB, 13,524 frame filenames) to resolve animation names to textures, derives `boundingBox` from `sprite.getBounds()`, and handles gravity, movement, jump and `onCollision`. `StickmanAnimationStateMachine` switches on `stickman.details.animation` (`idle` / `walk normal` / `jump B`). `src/Physics/Physics.ts` (57 lines) provides `isCollision` (AABB overlap → calls `onCollision` on both objects) and an unused impulse-based `collisionResponse`; `Physics/Constants.ts` holds `GRAVITY = 9`, `IMPULSE_POWER = 200`, `TERMINAL_VELOCITY = 10`.

### Stack

TypeScript 5.6 under `strict`, `noUnusedLocals`, `noUnusedParameters`, `noFallthroughCasesInSwitch`. Vite 5.4 with a hand-written plugin in `vite.config.mts` wrapping `@assetpack/core` and `pixiPipes` — texture packing at 0.4 resolution, PNG quality 95, output to `public/assets/`, watching in `vite dev` and running once on build. Runtime deps are only `pixi.js` 8.6 and `axios` 1.7.

## What works well

- The folder layout (`Scenes/`, `Sprites/`, `Physics/`, `InputHandling/`, `StateMachine/`) with `INode`, `IScene` and `IPhysicsObj` interfaces is a real architecture, not a single-file demo: scenes own lifecycle, sprites own rendering, physics owns collision.
- The AssetPack Vite plugin handles the sprite-sheet pipeline declaratively — `raw-assets/` is packed into Pixi bundles and a manifest on both dev and build.
- `resize()` in `main.ts` letterboxes by computing `min(winW/1280, winH/640)` and centring with margins, so the 2:1 virtual resolution never distorts.
- `playAnimationOnce` stores `prevAnimation`, sets `loop = false` and restores it from `onComplete`, clearing the handler so it can't fire twice; `setSkin`/`setWeapon`/`setAnimation` lazily call `initialize()` so callers can't hit an unready sprite.

## What I'd change

- **The asset set is broken.** `public/stickman-animations.json` indexes 10 skins × up to 5 weapons × ~30 animations (13,524 frame references), but `raw-assets/Stickman/` contains one folder — `BW{m}{tps}{fix}`, 159 PNGs. `Stickman.curSkin` defaults to `"Black"`, and the committed `.assetpack/122a2aab….json` cache proves ten skin folders (`Black`, `Blue`, `Red`, …) once existed and are now gone. Regenerating the manifest from the current tree cannot produce the `"Black"` bundle that `Assets.loadBundle("Black")` asks for.
- `src/main.ts` calls `init()` as a bare top-level call with no `.catch`. `public/assets` is gitignored, so if `axios.get('/assets/manifest.json')` fails the rejection is unhandled and the page stays a blank blue rectangle with no message.
- `Stickman.boundingBox` is a **getter with side effects**: when `showBoundingBox` is true it removes, destroys and re-creates a debug `Graphics` on every read. It is read twice in `onCollision` and once per frame from `Physics.isCollision`, so `sprite.getBounds()` runs three or four times per frame regardless.
- `Physics.collisionResponse` and `IMPULSE_POWER` are dead code — nothing in `src/` calls them.
- Gravity is faked rather than integrated: `velocity.y = Math.min(acceleration.y * dt, TERMINAL_VELOCITY)` never accumulates velocity (no `v += a * dt`), `onCollision` stops falling with `this.acceleration.y -= this.acceleration.y`, and since `GRAVITY = 9 < TERMINAL_VELOCITY = 10` the clamp never engages at 60 fps.
- `.assetpack/122a2aab….json` (7.4 MB) and `raw-assets/` (5.4 MB, 159 files) are tracked in git even though `.gitignore` lists both — the ignore rules came after the files. The AssetPack cache also embeds absolute Windows paths (`C:/Users/user/Documents/code projects/games/full games/combat-gods/…`).

## Still outstanding

- One commit: `f5be825 "added collision detection"`. No README at all (the GitHub description is empty).
- `TestState.leave()` calls `scene.leave()` but never `app.stage.removeChild(scene)` and never clears `this.scenes` — re-entering the state pushes duplicate scenes into the array.
- `StickmanAnimationStateMachine.handleJumpState` is an empty body; the jump state never transitions to anything.
- No tests, no linter/formatter config, no CI.
- Only horizontal movement and one ground plane exist — no second fighter, AI, blocking or health, despite `stickman-animations.json` indexing `jab`, `kick`, `uppercut`, `block`, `hurt` and `die` animations that nothing plays.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
