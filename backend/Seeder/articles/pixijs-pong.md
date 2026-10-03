# pixijs-pong

A two-player Pong in PixiJS 8 with a start screen, serve/play/win states, bitmap-style text and four WAV sound effects.

## What it does

Open the dev server and you get a start screen ("This is Pong!!!", controls for both players). Press <SPACE> to switch to the play scene. Player 1 moves with <W>/<S>, Player 2 with the arrow keys. Press <a> to serve; the ball accelerates by 5% on every paddle hit and picks a new random vertical angle. A point is scored when the ball passes a paddle edge, which plays `player_scores.wav` and returns to the serve state with the other player serving. First to 10 wins, plays `player_wins.wav`, and <a> resets the scores. A live FPS counter sits in the top-left.

## How it works

`src/index.ts` instantiates `App` with `window` and `document`. `src/App.ts` (88 lines) creates the Pixi `Application` sized to the window (`resizeTo: this.window`, `roundPixels: true`), then eagerly builds both scenes into `this.scenes: Record<GameSceneName, BaseScene>` — `PlayScene` and `StartScene`, constructed at startup, each at the fixed virtual size 432×237. `scaleScene` computes a scale and offset from `app.screen` once, at construction time.

Scene switching is done with a DOM `CustomEvent` rather than a direct call: `BaseScene.emitSceneChange` dispatches `"scene-change"` with `detail.newScene`, and `App.onSceneChange` swaps `currentScene`, calls `stage.removeChildren()` and re-adds the next scene. Each frame, `App.onUpdate` passes the `keysPressed` record into `currentScene.update(dt, keysPressed)`.

`src/scenes/PlayScene.ts` (156 lines) is where the game lives. It holds `gameState: "serve" | "play" | "win"` and switches on it in `update`, delegating to `serveStateUpdate`, `playStateUpdate`, `winStateUpdate`. Collision is AABB: `BaseSprite.isCollision` in `src/sprites/BaseSprite.ts` does four early-return overlap tests. `Ball.ts` (85 lines) owns velocity (`dx`, `dy`), the serve direction, wall bounce plus `ball_hit_wall.wav`, and `changeDirectionAfterCollision` which reverses `dx`, multiplies by 1.05, randomises `dy` and plays `paddle_hit_ball.wav`. `Paddle.ts` clamps movement to `[5, WINDOW_HEIGHT - height - 5]` and exposes `Score`.

Fonts in `src/fonts/` are four `TextStyleOptions` objects (Tiny 8px through Huge), not bitmap fonts. Scenes are plain `Container` subclasses; sprites are `Graphics` rectangles filled white.

### Stack

- **TypeScript 5.3**, strict, with `noImplicitOverride`.
- **PixiJS 8.1** for rendering; **webpack 5** + `ts-loader` for bundling (same config shape as pixijs-games-template).
- **HTML5 `Audio`** elements for sound — plain `new Audio("sounds/…")`, not Pixi's audio system.
- **pixi-filters 6.0** and **@pixi/sound 6.0** are both in `package.json` dependencies and neither is imported in `src/`.

## What works well

- The state machine in `PlayScene.update` is explicit and readable: three private methods, one `switch`, no mutation hidden in helpers.
- `isCollision` on `BaseSprite` is shared by `Ball` and both paddles, and the serve/play/win flow resets positions and scores through `Ball.reset()` rather than rebuilding the scene.
- Sound effects are attached to the exact event that causes them (wall bounce in `Ball.update`, score in `PlayScene.playStateUpdate`) rather than being polled.
- The virtual-resolution approach (432×237 scene space, scaled to the window) keeps gameplay deterministic regardless of window size.

## What I'd change

- **Scene scaling is computed once and never recomputed.** `App.scaleScene` runs inside `initializeApp`, using `app.screen` at that instant. There is no `resize` listener, so after the window changes size the stage keeps its old scale/offset while `Application.resizeTo` resizes the canvas — the playfield drifts off-centre and the wrong aspect ratio.
- **Both scenes are constructed at startup**, including four `new Audio(...)` objects in `PlayScene`/`Ball`, whether or not the player ever presses <SPACE>. Scenes should be created (or at least loaded) on entry.
- **`PlayScene.ts` is 156 lines mixing UI text layout, input handling, scoring and rules.** The score display, serve prompt and win prompt are laid out in the constructor and then reassigned every frame in `update` (`winningText.text = ""`, `servingText.text = ""`, then re-set in each branch) — text is churned on every tick.
- **`Paddle.onKeyDown` is declared `async` for no reason** (`Paddle.ts:34`), returning a promise nobody awaits; it also doesn't handle keys, it applies movement, so the name is wrong too.
- **`@pixi/sound` and `pixi-filters` are installed but unused**, while the actual audio path is raw `HTMLAudioElement`, which has no pooling — rapid `play()` calls on the same element cut each other off.

## Still outstanding

- No README at all: the repository has no `.md` file, so the control scheme only exists inside `StartScene.ts`.
- No tests: no test script, no runner, no `*.spec.ts`.
- `BaseScene` has no `destroy()`/teardown, and `App.onSceneChange` uses `removeChildren()` without destroying children, so returning to start from play leaks the previous scene's textures and audio elements.
- The entire history is one commit (`836a602 added sound`), with no TODO markers anywhere in the source — the unfinished parts are structural, not annotated.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
