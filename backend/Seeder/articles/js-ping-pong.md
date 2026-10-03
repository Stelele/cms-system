# js-ping-pong

An intended PixiJS ping-pong game that is, as committed, still the untouched webpack starter template.

## What it does

Loading the app opens a 640×480 cornflower-blue canvas with a single sprite (`clampy.png`) pinned to the center. There are no paddles, no ball, no input handling, no score — nothing ping-pong-related executes. The page `<title>` in `src/index.ejs` is still "Pixi Hotwire", the name of the generator template this repo was bootstrapped from.

## How it works

The whole game layer is `src/index.ts`, 18 lines:

- Creates a PixiJS `Application` bound to the existing `#pixi-canvas` element with `resolution: window.devicePixelRatio || 1`, `autoDensity: true`, background `0x6495ed`, 640×480.
- Builds `Sprite.from("clampy.png")`, sets `anchor` to (0.5, 0.5), centers it on `app.screen`, and adds it to `app.stage`.

That is the entire runtime. There is no game loop, no `ticker.add`, no state machine, no update function. `src/index.ejs` supplies the HTML shell (viewport meta, `#pixi-content` wrapper, noscript fallback) and `static/` holds only template assets: `clampy.png`, `clampy.svg`, `favicon.ico`.

`webpack.config.js` (84 lines) does the real work of the repo: `ts-loader` compiles the TypeScript, `html-webpack-plugin` injects the EJS template, `copy-webpack-plugin` moves `static/` into `dist`, and `terser-webpack-plugin` minifies production builds. Scripts are `start` (webpack-dev-server), `build` (clean → build-only), `build-only`, `clean`.

### Stack

PixiJS 7.2.4 as the sole runtime dependency; TypeScript 5.1, webpack 5.88, `ts-loader`, `html-webpack-plugin`, `copy-webpack-plugin`, `npm-run-all`, and `rimraf` as dev tooling — a full bundler pipeline wrapped around 18 lines of code.

## What works well

- The build toolchain is real and complete: dev server, production build with minification, asset copying, TypeScript strict config — a proper foundation if the game were ever written.
- `package.json` pins everything with lockfile-backed ranges, and the license file is present.

## What I'd change

- **There is no game.** The single most notable gap: `src/index.ts` contains no ball, no paddles, no collision, no score, no input — just the template's centered sprite. Anything written about gameplay for this repo would be invented.
- The repo should either be renamed or reduced to a template/fork note until the implementation exists; as-is the name `js-ping-pong` promises something the source does not contain.
- `src/index.ejs` still titles the page "Pixi Hotwire" and `package.json` has `"license": ""` despite a `LICENSE` file sitting next to it.
- PixiJS 7.2.4 is a major version behind the 8.x used in the sibling projects (`pixijs-breakout`, `pixijs-flappy-bird`), so the template doesn't even share a baseline with the rest of the codebase.

## Still outstanding

- Literally everything gameplay-related: paddle input, ball movement, collision, scoring, win/lose states, sound, AI opponent.
- No README, no tests, no CI — a single commit titled "initial commit" and no further history.
- `tsconfig.json` and `webpack.config.js` exist but nothing exercises them beyond rendering one sprite.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
