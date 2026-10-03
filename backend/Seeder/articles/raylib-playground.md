# raylib-playground

A Go + raylib scratchpad holding seven small procedural-graphics experiments — cellular automata, random walks, histograms and Perlin noise — behind a single switchable draw loop.

## What it does

`go run .` (or `air`, configured in `.air.conf`) opens an 800×640 resizable window titled "Random Stuff" with a 60 FPS cap. Which experiment you see is decided by hand: `stuff/stuff.go` has one active line in each of `Init`, `Draw` and `Update`, and everything else is commented out. The active demo at time of writing is the 3D Perlin one — a 100×100 grid of cubes whose heights are a three-octave fBm that scrolls upward over time, drawn with `rl.DrawCube` plus a wireframe pass.

The other demos, selectable by uncommenting:

- **Conway** (`stuff/conway.go`): Game of Life on a 100×80 tile grid, seeded from `rand.ExpFloat64()`, stepping at 3 times a second, with left-click painting cells.
- **Random walk** (`stuff/random-walk.go`): a single walker leaving a trail on the shared tile grid.
- **Normal dist** (`stuff/normal-dist.go`): a 20-bin histogram of normally distributed samples.
- **Perlin walk** (`stuff/perlin-random-walk.go`): a walker whose direction comes from `perlin.Noise1D`.
- **2D Perlin** (`stuff/2d-perlin-noise.go`): greyscale value noise written into an 800×640 byte array and blitted one pixel at a time with `rl.DrawPixel`.
- **3D Perlin** (`stuff/3d-perlin-noise.go`): the cube field described above.

## How it works

`main.go` (76 lines) owns the window and camera: `rl.InitWindow(screenWidth, screenHeight, …)`, a perspective `rl.Camera3D` at (120, 50, 120) looking at the origin, `rl.SetWindowState(rl.FlagWindowResizable)`, then a `for !rl.WindowShouldClose()` loop that calls `stuff.Update()` between `BeginDrawing`/`EndDrawing` and dispatches to `draw3D()`.

There is no scene graph or entity system. All state lives in package-level globals inside `package stuff`:

- `tiles []bool` of `COLS*ROWS` declared in `stuff/stuff.go`, shared by Conway and the random walk.
- `curFrame` declared in `conway.go` but reset from `3d-perlin-noise.go`.
- `t` declared in `2d-perlin-noise.go` but reset by `PerlinNoise3DInit`.

Each demo is a triple of `XxxInit`/`XxxDraw`/`XxxUpdate` functions with no shared interface, so `stuff.go` is three functions of commented-out calls. `constants/constants.go` fixes `TILE_WIDTH`/`TILE_HEIGHT` at 8 and `COLS`/`ROWS` at 100/80, giving the 800×640 virtual resolution that `scaleContentsToWindow()` would map to the real window if it were used.

### Stack

- **Go 1.23.4**, module `ray-random` (`go.mod`).
- **github.com/gen2brain/raylib-go/raylib** for window, input, 2D/3D drawing.
- **github.com/aquilax/go-perlin** for value noise; octaves/alpha/beta are hard-coded as `perlin.NewPerlin(2, 2, 3, seed)`.
- **Air** (`.air.conf`) for hot reload, building to `./tmp/main.exe`.

## What works well

- The demo triple pattern (`Init`/`Draw`/`Update`) keeps each experiment self-contained: `conway.go` is 110 lines and you can read it without understanding the others.
- `updateTiles()` in `conway.go` correctly double-buffers — it writes into `tmpTiles` and swaps, so neighbour counts are never affected mid-generation.
- `clamp()` guards the mouse-paint index so clicking the window edge can't write outside `tiles`.
- The 3D Perlin update is a clean fBm: three `Noise3D` samples at `freq`, `freq*2`, `freq*3` weighted 1 / 0.5 / 0.25, recomputed each frame.

## What I'd change

- **The repo has no `.gitignore`, and an 11.7 MB Windows binary (`tmp/main.exe`) plus `tmp/air_errors.log` are committed.** That is build output from Air's own `tmp_dir`; every clone carries it.
- **Demo selection is commented-out code.** `stuff/stuff.go` requires editing three separate functions in lockstep (a mismatch — e.g. `ConwayInit` with `PerlinNoise3DDraw` — compiles and runs wrong). An enum or a `--demo` flag would remove the whole class of mistake.
- **Globals are shared across files in non-obvious ways**: `tiles` is declared in `stuff.go` and mutated by `conway.go`, `curFrame` is declared in `conway.go` and read by `3d-perlin-noise.go`, and `t` is declared in `2d-perlin-noise.go`. Moving between demos means knowing which file owns which variable.
- **Dead code is left in place**: `draw2D()` and `scaleContentsToWindow()` in `main.go` are never called (the loop uses `draw3D()`), and `float32ToByteArray` in `3d-perlin-noise.go` is never referenced.
- **`PerlinNoise3DInit` allocates an image, a texture, a mesh and a model that `PerlinNoise3DDraw` never uses** — it only calls `DrawCube`. That is a loaded texture and a model leaked every time the demo is initialised.

## Still outstanding

- No README and no tests: no `*_test.go` anywhere in the repo.
- `draw2D`'s virtual-resolution path is written but unreachable, so the 2D demos currently draw in raw window coordinates and don't scale with the resizable window.
- `PerlinNoise2DInit()` has an empty body — the 2D Perlin demo has no setup step at all.
- `.air.conf` watches `tsx` and `tmx` extensions that do not exist in this repository.
- Single commit (`c5d74c1 added more stuff`); nothing in the source is marked TODO, so the commented-out `freq`-remapping block in `3d-perlin-noise.go` is the only hint of an abandoned idea.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
