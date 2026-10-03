# pixijs-breakout

A Breakout-style arcade game in TypeScript and PixiJS 8, built on a scene/state architecture with bundle-based asset loading.

## What it does

A menu screen shows a "Breakout" title with START and HIGH SCORES options navigable by arrow keys; Enter on START starts the play scene. In play, a paddle moves with left/right arrows and a ball bounces off the walls, the top edge, and the paddle (each bounce plays a sound and randomizes horizontal direction). That is where the gameplay currently ends — there are no bricks, no score counter, no lives, and no way to win or lose.

## How it works

`src/index.ts` is one line: `Manager.initialize(640, 320, 0x000000)`.

`src/Manager.ts` (138 lines) is the static singleton: it creates the PixiJS `Application` on `#pixi-canvas`, calls `Assets.init({ manifest })`, kicks off `Assets.backgroundLoadBundle`, adds a persistent `BackgroundScene` with its own ticker callback, wires `window.resize` (CSS letterboxing that scales the canvas to fit), and starts `StateMachine.change("start")`. Scenes implement the `IScene` interface (`assetBundles`, `assetsReady`, `constructorWithAssets`, `update`, `destroyAssets`); `changeScene()` awaits the asset promise, loads the scene's bundles, then swaps the stage child.

`src/stateManagement/StateMachine.ts` (30 lines) maps `'null' | 'start' | 'play'` to state factories. Each state owns a scene: `StartState` builds `StartScene` and, when `assetsReady`, polls `InputManager.keysPressed["Enter"]` and only transitions if `selection === "START"`; `PlayState` builds `PlayScene` and forwards the ticker.

`src/scenes/PlayScene.ts` (63 lines) creates `PaddleSprite` and `BallSprite`, positions them once `positionAssets()` confirms textures parsed, and each tick runs AABB collision via `helpers/IsCollision.ts` to flip `ballSprite.dy`, reposition, and set a new random `dx`.

`src/sprites/PaddleSprite.ts` (88 lines) and `BallSprite.ts` (89 lines) both hand-build `SpritesheetData` frame rectangles out of `breakout.png` (4 paddle skins × 4 sizes; 7 ball frames) rather than using an atlas JSON. `Manifest.ts` declares four bundles (`background-scene`, `play-scene`, `start-scene`, `fonts`) consumed through `Assets.loadBundle`.

### Stack

PixiJS 8.1 and `@pixi/sound` 6.0 as runtime dependencies; TypeScript 5.3, webpack 5.90 with `ts-loader`, `html-webpack-plugin`, `copy-webpack-plugin`, `terser-webpack-plugin`, and `npm-run-all` for the build (`npm start` runs webpack-dev-server).

## What works well

- The scene/state split is real and consistent: `IScene` forces asset-bundle declaration and a `destroyAssets` teardown, and states own their scenes cleanly.
- Bundle-based loading with background preloading (`Assets.backgroundLoadBundle`) plus a `BackgroundScene` that checks `assetsReady` avoids first-transition jank.
- Sprite frame geometry is derived programmatically (loops over `range(0, 4)` in `PaddleSprite.getSpriteSheetData`) instead of hardcoded frame-by-frame, and the resize handler in `Manager.ts` does proper uniform-scale letterboxing.

## What I'd change

- **The game cannot end.** `BallSprite.update` reflects only x-walls and the top edge; there is no bottom bound. Miss the paddle and the ball falls off-screen forever — `PlayScene.update` never resets or removes it. Combined with the complete absence of bricks (grep for `brick|score|lives` in `src/` only matches the menu's HIGH SCORES text), the loop is paddle-bounce indefinitely.
- **Dead menu entry.** `StartState.update` guards the transition with `selection === "START"`, so selecting HIGH SCORES + Enter does nothing; there is no high-score code anywhere.
- **Unused assets ship anyway:** `static/sounds/` contains `brick-hit-1/2.wav`, `victory.wav`, `high_score.wav`, `hurt.wav`, `recover.wav`, `pause.wav`, `music.wav`, and `static/images/hearts.png` — none are referenced in `src/` or `Manifest.ts`.
- Resource leaks on scene change: `PlayScene.destroyAssets()` destroys only `_paddleSprite`, never `_ballSprite`; and `positionAssets()` recurses via `setTimeout(fn)` with no delay until textures resolve, masking an async ordering bug rather than awaiting `spriteSheet.parse()`.
- Sounds bypass the bundle system entirely — `BallSprite` and `StartScene` use `Sound.from("sounds/…")` by raw path even though the aliases are declared in `Manifest.ts`.

## Still outstanding

- Bricks, levels, scoring, lives, win/lose flow, and any high-score storage.
- No README beyond two lines ("A augmented template made using the principles from Pixi JS Elementals"), no tests, no CI workflow (no `.github/` at all), single commit "made bounce update".
- Page `<title>` in `src/index.ejs` is still "Game Template".

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
