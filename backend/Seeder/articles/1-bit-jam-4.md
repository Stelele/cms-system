# 1-bit-jam-4

A browser game skeleton — Vite, TypeScript and PixiJS 8 — built as the entry "Tower of Light and Shadow" for the itch.io 1-BIT Jam #4 (26 Sep – 11 Oct 2024).

## What it does

Opens a full-window black canvas and drops you in as a solid white 20×20 rectangle. `A` and `D` walk it left and right, `Space` jumps. Gravity pulls it down to a floor line at `window.innerHeight - 100`, and that's the whole game. There is no level, no title screen, no score, no light mechanic, and nothing to interact with.

## How it works

`index.html` loads `src/main.ts`, which constructs an `App` (src/app.ts) and calls `start()`. `App.start()` awaits `pixi.init({ resizeTo: window })`, appends the canvas, calls `Input.init()`, sets `StateMachine.setState(new StartState())`, and registers `StateMachine.update` on the Pixi ticker.

`state-machine/StateMachine.ts` is a static holder for one `IState` (`enter` / `leave` / `update`). `state-machine/states/StartState.ts` is the only state: it news up a `TestScene`, adds it to the stage on `enter()` and removes it on `leave()`. `scenes/TestScene.ts` is a `Container` that owns one `PlayerSprite`. `sprites/PlayerSprite.ts` is a `Graphics` subclass drawing one white rect; its `update(dt)` runs `handleGravity` → `handleInput` → `handleJump`. `utils/InputHandler.ts` records raw `KeyboardEvent.code` values into a static `keys` map plus a `debounceKeys` map that a 300 ms `setTimeout` clears.

### Stack

TypeScript 5.5 under `strict`, `noUnusedLocals` and `noUnusedParameters` (tsconfig.json). Vite 5.4 for dev and `tsc && vite build` for production — there is no `vite.config.*`, so it runs on Vite defaults. `pixi.js` 8.4.1 is the only runtime dependency. Design notes live in `docs/Game Design Doc.md` and a 504 KB `docs/Gameplay ideas.pdf`.

## What works well

- The whole codebase is 194 lines across nine files, and every file has exactly one job: app, state machine, one state, one scene, one sprite, one input module.
- `IState` and `IUpdatable` (src/types/IUpdatable.ts) keep the state machine ignorant of scenes and sprites — `StartState` is the only place that knows `TestScene` exists.
- `PlayerSprite.input` is a private getter translating raw codes (`KeyA`, `KeyD`, `Space`) into semantic names, so the sprite never touches `event.code` directly.
- `tsconfig.json` turns on `strict`, `noUnusedLocals`, `noUnusedParameters` and `noFallthroughCasesInSwitch` — stricter than the Vite template default.
- `docs/Game Design Doc.md` actually defines an MVP ("3 levels with puzzles and platforming in place") and a USP, so the intent was written down even though the code isn't there yet.

## What I'd change

- Almost nothing from the design is implemented. `grep` across `src/` finds no light, shadow, level, collision or puzzle code — the GDD's stated USP is "Mess with realistic light in a 2d environment" and there is not a single `Light`/`Shadow` identifier in the repo. The only scene is literally called `TestScene`.
- `scenes/TestScene.ts` has a bare `this.x` statement in its constructor — a leftover no-op that reads a property and discards it.
- Jump timing is frame-coupled: `handleJump` decrements `jumpTime` by `1` per tick instead of by `dt.deltaTime`, so hang time changes with refresh rate.
- The "can I jump?" test is an emergent side effect of call order. `update()` runs `handleGravity` first (which sets `prevY = this.y` and then increases `y`), and `isFalling` is `this.prevY - this.y < 0`. In mid-air that is always true and blocks the jump; on the floor the clamp makes it exactly `0` and allows one. That works, but only because gravity happens to run before input — it is not an explicit on-ground check.
- `handleInput` moves `position.x` with no clamp at all, so the player can walk off both sides of the screen. The floor is a magic `app.height - 100` inside `handleGravity`, not a level object.
- `Input.debounceKeys` schedules a fresh `setTimeout` on every `keydown` (OS key repeat included) and never cancels the previous one; and because `Input.init()` is called from `App.start()`, calling `start()` twice would double-register the window listeners.

## Still outstanding

- One commit only: `5a569bd "basic set up of the game dev environment and creation of player character"`.
- Every section of `docs/Game Design Doc.md` except Target, Marketing and MVP is just "Check [Gameplay ideas pdf]" — story, gameplay, level design, art, controls and audio were never written down outside the PDF.
- No tests, no linter, no formatter, no CI, no `vite.config`, and `public/` contains only the stock `vite.svg`.
- `state-machine/states/` contains only `StartState.ts` — there is no pause, game-over, win or menu state despite the state machine being the central abstraction.
- No sprites or art assets of any kind; the player and scene are Pixi `Graphics` primitives.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
