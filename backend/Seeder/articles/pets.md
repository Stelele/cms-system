# pets

A tiny Go + raylib experiment: open a window, load a Tiled map, and draw it. 202 lines of Go across three files — this is a rendering scaffold, not yet a game.

## What it does

Run it and a 480×320 window opens (resizable), a TMX tilemap from `assets/Tilemaps/test-map.tmx` is drawn centred and letterboxed to whatever size the window is, and the loop runs at 60 FPS until you close the window. Nothing else happens: there is no player, no input handling, no collision. The assets folder has Player, Enemies, Animals and Outdoor decoration sprites, none of which are referenced by any code.

## How it works

`main.go` (57 lines) is the entry point. It calls `rl.InitWindow(480, 320, …)`, sets the window resizable, calls `g.InitAsset()`, then runs the loop: `g.Update()` → `BeginDrawing` → `scaleContentsToWindow()` → `BeginMode2D(camera)` → `g.Draw()` → `EndMode2D`. `scaleContentsToWindow` computes `min(screenW/worldW, screenH/worldH)` and builds a `rl.Camera2D` with that zoom and a centring offset, so the fixed 30×20 tile world (480×320 px, matching the map) scales to any window size.

`game/game.go` (105 lines) does the real work. `InitAsset()` calls `loadSceneData("assets/Tilemaps/test-map.tmx")`, which `xml.Unmarshal`s the TMX into the package-level `testScene types.TileMap`, then opens each referenced `.tsx` tileset file, copies its metadata (`Columns`, `TileCount`, `TileWidth`, …) into `testScene.TileSets[idx]`, rewrites the image path to strip Tiled's `../`, and `rl.LoadTexture`s it. `Draw()` walks both layers, splits the CSV `<data>` on every call, converts each cell with `strconv.Atoi`, finds the tileset whose `[FirstGID, FirstGID+TileCount)` range contains the gid (`getTileSet`), computes the `u`/`v` source cell, and calls `rl.DrawTextureRec`.

`game/types/tilemap.go` (40 lines) is the data model: `TileMap`, `TileSet`, `Image` (carrying an unserialised `rl.Texture2D`) and `Layer`, all with Tiled XML attribute tags.

### Stack

Go 1.23.4 and `github.com/gen2brain/raylib-go/raylib` (the only real dependency; `purego`, `golang.org/x/sys` and `x/exp` come in transitively). Tiled 1.11.2 for map authoring, `.air.conf` for hot reload, and Kenney's Cute Fantasy asset pack.

## What works well

- The camera scaling in `main.go:41-57` is correct and clean — uniform zoom plus centring offset, so resizing never distorts the map.
- The TMX/TSX split is handled properly: external tileset files are merged back into `testScene.TileSets` and relative image paths fixed up, which is the fiddly part of Tiled integration.
- Data lives in `types.TileMap` with XML tags, keeping parsing separate from drawing.

## What I'd change

- **Every error in `loadSceneData` is discarded** (`game.go:40-52`: `sceneFile, _ := os.Open(...)`, `io.ReadAll`, `xml.Unmarshal`). Worse, `defer sceneFile.Close()` runs on a nil file handle if `Open` fails — a missing `test-map.tmx` panics instead of reporting.
- `getTileSet` (`game.go:25`) returns a zero-value `TileSet` for an unmatched gid, and `Draw` then divides by `tileSet.Columns` (`game.go:92`) — a 0 there is an integer divide-by-zero panic. There is no default tileset.
- `Draw()` re-splits and re-parses every layer's CSV on every frame (`game.go:73`), at 60 FPS for 600 cells × 2 layers. Parse once in `loadSceneData` into an `[]int32`.
- The map size is hard-coded as `30*16`/`20*16` constants in `main.go:10-14` rather than read from `testScene.Width/Height`, so a differently sized map draws off-centre.

## Still outstanding

- `Update()` is an empty function (`game.go:67-69`) — no input, movement, or game loop logic; the window title is still raylib's example default, `"raylib [core] example - basic window"`.
- Zero tests, no README, no LICENSE — and `assets/read_me.txt` states the Cute Fantasy pack is **non-commercial only**, so the project can't ship commercially without different art.
- A single commit ("fixed scaling of contents to current window size").

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
