# learn-pixi-js

A minimal PixiJS v7 sandbox: one 640×480 stage with a single sprite (`clampy.png`) centred on it, wrapped in a webpack + TypeScript dev setup.

## What it does

`npm start` serves the bundle on `http://localhost:1234` (webpack-dev-server, bound to `0.0.0.0`). The page is a cornflower-blue (`0x6495ed`) 640×480 canvas with one image — `clampy.png` from `static/` — anchored at its centre and positioned in the middle of the screen. It does not move, react to input, or resize.

## How it works

The whole application is `src/index.ts`, 18 lines:

```ts
const app = new Application<HTMLCanvasElement>({
  view: document.getElementById("pixi-canvas") as HTMLCanvasElement,
  resolution: window.devicePixelRatio || 1,
  autoDensity: true,
  backgroundColor: 0x6495ed,
  width: 640, height: 480
});
const clampy: Sprite = Sprite.from("clampy.png");
clampy.anchor.set(0.5);
clampy.x = app.screen.width / 2;
clampy.y = app.screen.height / 2;
app.stage.addChild(clampy);
```

There is no `app.ticker` callback, no event handlers, no `resizeTo`. The `Application` is constructed against an existing `<canvas id="pixi-canvas">` rather than letting Pixi create one.

The shell around it is template scaffolding. `src/index.ejs` (title still `Pixi Hotwire`) supplies `#pixi-content > canvas`, and `webpack.config.js` (84 lines) is the only build logic: `entry: './src/index.ts'`, `ts-loader` for `.ts/.tsx`, `CopyPlugin` copying `static/` to `dist/`, `HtmlWebpackPlugin` inlining `src/index.ejs` with `hash: true`, Terser with `drop_console` in production, and `devtool: 'eval-source-map'` only in development.

`static/` holds `clampy.png`, `clampy.svg` and `favicon.ico`; `CopyPlugin` is what makes the bare `Sprite.from("clampy.png")` URL resolve.

### Stack

- **pixi.js 7.4.0** — the only runtime dependency; everything else is build tooling.
- **webpack 5 + ts-loader + TypeScript 5.3** — `tsconfig.json` is notably strict (`strict`, `noUnusedLocals`, `noUnusedParameters`, `noImplicitOverride`, `noFallthroughCasesInSwitch`), though it guards 18 lines of code.
- **webpack-dev-server 5** on port 1234 with `overlay.errors` on and `overlay.warnings` off.
- **npm-run-all / rimraf** for the `clean` → `build-only` production chain.

## What works well

- It is a working, reproducible starting point: `npm start` gives you HMR on a real canvas with no framework ceremony, and the strict `tsconfig.json` means any code added later is immediately type-checked under `noUnusedLocals`/`noImplicitAny`.
- The sprite is loaded with `Sprite.from("clampy.png")`, so Pixi handles the texture from the copied static assets — no manual `Assets.add`/`Assets.load` boilerplate for the simple case.
- Terser is configured with `compress: { drop_console: true }` for production only, so debug logging costs nothing in a shipped build while still working in dev.
- `performance: { hints: false }` is set with a comment explaining it is for web games — the intent of the template is clear.

## What I'd change

- **The repo is an unmodified starter template.** `package-lock.json` still has `"name": "pixi-hotwire"`, `src/index.ejs` still has `<title>Pixi Hotwire</title>`, and `package.json` has no `"name"` field at all with `"license": ""`. None of it was renamed to this project.
- **`webpack.config.js` ships an explicitly unsafe setting with the warning still in it**: `allowedHosts: "all", // If you are using WebpackDevServer as your production server, please fix this line!` alongside `host: '0.0.0.0'`. Anyone who follows the file literally exposes the dev server to the network.
- **There is no README.** The repo has ten files and none of them explains what this is, how to run it, or what was learned.
- **Nothing exercises the renderer**: no `app.ticker.add`, no `resizeTo` / resize listener, so the fixed 640×480 stage just sits in the top-left of a full-screen container once the window is bigger than 640×480.
- `webpack.config.js` is 84 lines of copied config for an 18-line program — sourcemaps, Terser options, copy plugin and HTML templating could be replaced by a ~10-line Vite config, which is what the author's later projects use.

## Still outstanding

- **No tests and no `test` script** — `package.json` defines only `start`, `build`, `build-only` and `clean`.
- No lint or format configuration (no ESLint, no Prettier).
- No CI workflow.
- Nothing is marked `TODO`/`FIXME`/`HACK` in the source; the only evidence of unfinished work is that the template's own leftovers (title, package name, dev-server warning) were never cleaned up.
- `static/clampy.svg` is copied into every build by `CopyPlugin` but never referenced — the code loads only `clampy.png`.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
