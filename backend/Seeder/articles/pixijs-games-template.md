# pixijs-games-template

A small PixiJS v8 starter kit: a static `Manager` that boots the app, loads asset bundles, and keeps a scrolling parallax background on screen while the game logic is left for the next project.

## What it does

Running `npm start` (webpack-dev-server on port 1234) opens a 640×320 black canvas with two horizontally scrolling strips — `static/images/background.png` and `static/images/ground.png` — looping at different speeds behind everything. The canvas is letterboxed to fit the browser window while keeping the fixed virtual resolution. Keyboard events are captured globally and exposed as a `keysPressed` record.

There is no game on top of that yet: no player, no scenes beyond the background, nothing to press.

## How it works

Entry point is `src/index.ts`, which does one thing: `Manager.initialize(640, 320, 0x000000)`. `src/Manager.ts` (139 lines, the largest file in the repo) is a fully static singleton. It creates the Pixi `Application` on the `#pixi-canvas` element declared in `src/index.ejs`, calls `Assets.init({ manifest })` using the bundle list in `src/Manifest.ts` (a single bundle named `background-scene`), kicks off `Assets.backgroundLoadBundle` so later bundles download while the first scene runs, then wires the Pixi ticker to `Manager.update`, which does `StateMachine.update(ticker)` followed by `InputManager.reset()`.

`IScene` (exported from `Manager.ts`) is the contract scenes implement: `assetBundles`, `assetsReady`, `constructorWithAssets()`, `update(ticker)`, `destroyAssets()`. `Manager.changeScene` loads a scene's bundles, destroys the previous scene, then calls `constructorWithAssets()`. The only implementation is `src/scenes/BackgroundScene.ts`, which instantiates `BackgroundGraphic` and `GroundGraphic`. Each graphic builds a `GraphicsContext` containing the same texture twice, translated by `texture.width`, and slides `position.x` by `ticker.deltaMS * SCROLL_SPEED` modulo half its width — the standard two-tile scroll loop.

`src/stateManagement/StateMachine.ts` is a second, separate switching mechanism: a static `Record<string, () => IState>` of state factories with `enter`/`exit`/`update` (interface `IState` in `states/BaseState.ts`, plus a no-op `NullState`). Scenes and states are independent systems; nothing connects them.

### Stack

- **TypeScript 5.3** with `strict`, `noUnusedLocals`, `noImplicitOverride` on (`tsconfig.json`).
- **PixiJS 8.1** for rendering, `Assets` bundles for loading, `Graphics`/`GraphicsContext` for the scrolling strips.
- **@pixi/sound 6.0** is declared in `package.json` but never imported anywhere in `src/`.
- **Webpack 5** with `ts-loader`, `copy-webpack-plugin` for `static/`, and `html-webpack-plugin` for the EJS template — not Vite.

## What works well

- `Manager.ts` keeps scaling logic in one place: `resize()` computes a uniform scale, sets CSS width/height and margins, and never touches game coordinates, so the virtual 640×320 space stays stable across window sizes.
- Bundle loading is asynchronous but ordered: `initializeAssetsPromise` is awaited by both `addBackgroundScene` and `changeScene`, so a scene can't be constructed before `Assets.init` resolves.
- The `IScene` interface forces every scene to declare its own bundles and to expose `assetsReady`, which `backgroundUpdate` checks before touching graphics — a real guard against animating half-loaded textures.
- `tsconfig.json` is stricter than the Vite defaults in the author's other repos (`noImplicitOverride`, `noFallthroughCasesInSwitch`).

## What I'd change

- **The state machine ships empty.** `StateMachine.change("title")` is called in `Manager.ts:65`, but `StateMachine.ts` only registers `'null'`. The lookup fails, the `else` branch silently installs `NullState`, and every subsequent `update` is a no-op. The game boots into a dead state machine with no error or warning.
- **`Manager.changeScene` is dead code.** Nothing in `src/` calls it, and there is no scene other than `BackgroundScene` to change to. The scene lifecycle the interface describes (`destroyAssets`, bundle unloading) is written but never exercised.
- **Two overlapping orchestration systems.** `StateManager` and the scene/`changeScene` path both drive "what runs each frame"; a new game would have to pick one and delete the other.
- **Unused exports.** `src/helpers/RandomNumbers.ts` exports `randomInt`/`randomNum`, re-exported through `helpers/index.ts`, and neither is referenced anywhere in `src/`. `IGraphics` is implemented by both graphics classes but never consumed.
- **`@pixi/sound` is a dependency that is never imported**, so audio has to be wired from scratch.

## Still outstanding

- No tests of any kind: no test script in `package.json`, no test runner, no `*.test.ts`.
- No linter or formatter config — only `.vscode/settings.json`.
- The `ReadMe.md` is two lines ("A augmented template made using the principles from Pixi JS Elementals") with no setup, no architecture notes, and no description of the scene/state split.
- `BackgroundScene.destroyAssets()` is never called, so nothing validates that scene teardown actually works.
- The repo has a single commit (`c0879e2 initial commit`), so there is no history showing what was intended next.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
