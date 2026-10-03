# pixijs-flappy-bird

A Flappy Bird clone ("Fifty Bird") in TypeScript and PixiJS 8, using the same scene/state shell as the sibling breakout project but with a complete title → countdown → play → game-over loop.

## What it does

Title screen prompts "Press Enter"; a 3-second countdown scene ("Press space to jump") then drops into play, where the bird falls under gravity and space applies an upward impulse. Pipes spawn from the right every 2.5 seconds with a 90px gap; passing one increments the on-screen score and plays a coin sound. Touching a pipe plays explosion/hurt sounds and transitions to a score screen showing the run's score with Enter to retry and Escape for the title. A scrolling ground and looping background music run behind everything.

## How it works

`src/index.ts` calls `Manager.initialize(640, 320, 0x000000)`. `src/Manager.ts` (139 lines) — near-identical to the breakout version — creates the PixiJS app, initializes the `manifest`, background-loads bundles, adds the persistent `BackgroundScene` on its own ticker, handles resize letterboxing, and boots `StateMachine.change("title")`. Its `update()` also calls `InputManager.reset()` each tick, turning the key map into edge-triggered input.

`src/stateManagement/StateMachine.ts` (29 lines) registers four states — `title`, `count-down`, `play`, `score` — and throws on an unknown name. Each state wraps one scene; `PlayState.update` is where death is detected: `playScene.pipeManager.collides(playScene.bird)` → explosion + hurt → `StateMachine.change("score")`.

Scenes live in `src/scenes/`: `TitleScene`, `CountDownScene` (a `timer -= ticker.deltaMS` countdown from 3000ms with a `timerDone` flag), `PlayScene`, `ScoreScene`, and `BackgroundScene`. `PlayScene` (67 lines) owns `BirdSprite`, `PipeManager`, a `Text` score readout, and exports a module-level `export const scoring = { score: 0 }` that `PlayState.enter` zeroes and `ScoreScene` reads back.

`src/sprites/BirdSprite.ts` (29 lines) applies `GRAVITY = 0.02 * deltaMS` each tick, sets `dy = -5` on space, and clamps y to `[0, Manager.height - 50]`. `PipeManager.ts` spawns every 2500ms, updates all pipes, and filters out `shouldDestroy` ones. `PipeSprite.ts` (74 lines) builds an upper/lower pair from one `pipe` texture (upper is `scale.y *= -1`), scrolls at `-0.06 * deltaMS`, and does AABB collision with hand-tuned insets (`-13`, `+5`). `GroundGraphic.ts` scrolls by mirroring the ground texture twice and wrapping `position.x`.

`Manifest.ts` declares bundles `background-scene`, `background-music`, `play-scene`, and `fonts`.

### Stack

PixiJS 8.1 and `@pixi/sound` 6.0; TypeScript 5.3 with webpack 5.90 (`ts-loader`, `html-webpack-plugin`, `copy-webpack-plugin`, `terser`). CI: `.github/workflows/static.yml` builds with `npm ci && npm run build` and deploys `dist/` to GitHub Pages on every push to `master`.

## What works well

- Full game loop with four distinct states, a real death transition, retry, and return-to-title — the shell actually closes.
- Asset pipeline is coherent: bundle manifests, `assetsReady` guards, a persistent background scene with preloaded music, and PNG sprites rather than atlas math.
- Pipes are self-cleaning (`shouldDestroy` + filter in `PipeManager.update`) and the collision check is centralized in `PipeManager.collides`, so `PlayState` has one line of death logic.
- The Pages workflow means the game is actually shipped, not just committed.

## What I'd change

- **Ground contact is not death.** `BirdSprite.update` clamps `position.y` to `Manager.height - 50` (the top of the ground), so the bird lands and slides along the floor indefinitely; only pipe collisions end the run. A landing Flappy Bird is wrong, and it also lets players bypass pipe gaps by riding the ground.
- **Global mutable score:** `scoring` is a module-level object exported from `PlayScene.ts` and mutated by `PlayScene.update`, read by `ScoreState`/`ScoreScene`, and reset in `PlayState.enter`. Scene-to-scene communication through an importable singleton is exactly what the state machine exists to avoid.
- Sound loading ignores the manifest: `PlayScene` does `Sound.from("sounds/explosion.wav")` etc. and `BackgroundScene` does `Sound.from("sounds/music.mp3")` directly, so the `background-music` bundle (and its alias) is loaded but unused. Same for jump/hurt/score.
- `PipeSprite.isUpperCollision`/`isLowerCollision` are packed with magic offsets (`bird.x + bird.width - 13`, `bird.x + 5`) with no explanation or tests — these numbers define the entire feel of the game.
- `PlayScene.destroyAssets()` destroys the bird and pipe manager but not `_scoreText` or the three `Sound` instances.

## Still outstanding

- No best-score persistence — `ScoreScene` shows only the current run; `fonts/font.ttf` (alias `Font`) is declared in the manifest but never referenced by any font style.
- No README, no tests, no lint script.
- Difficulty never ramps: spawn interval is a fixed 2500ms and scroll speed a fixed `-0.06` with no progression.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
