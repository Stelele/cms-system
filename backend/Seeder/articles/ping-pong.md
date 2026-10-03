# ping-pong

A classic two-player Pong clone for LÖVE (Love2D), written in Lua with a virtual-resolution scaling layer and a four-state game loop.

## What it does

Window opens at 1280×720 (resizable) with a 432×243 virtual playfield letterboxed inside it. Enter advances through `start` → `serve` → `play`; the serving player's direction is forced by `Ball:setDirection`, scores display as 32px mono digits, and the first player to 10 triggers `done` with a "Player N wins !!!!!" message — Enter then resets both scores and picks a new server. Escape quits at any time. Player 1 is W/S, player 2 is up/down arrows. Paddle hits, wall hits, and scores each play a `.wav`.

## How it works

`main.lua` (40 lines) is a thin shell: it calls `push:setupScreen(VIRTUAL_WIDTH, VIRTUAL_HEIGHT, ...)` using the vendored `libraries/push.lua`, constructs a single `GameBoard`, and forwards `love.keypressed`, `love.update`, `love.draw`, and `love.resize` into it (drawing happens between `push:start()` and `push:finish()`).

`assets/GameBoard.lua` (129 lines) is the hub: it loads fonts (`SMALL_FONT` 8px, `SCORE_FONT` 32px mono), builds `Paddle(…)` for both players, rolls `servingPlayer = math.random(2)`, creates the `Ball`, loads `paddle_hit`/`score` sounds, and holds `gameState`. `keyPressed` is the state machine; `update(dt)` moves paddles, checks `Ball:collides` against each paddle (playing `paddle_hit` and calling `invertDirection`), then calls score detection; `render()` is a ~40-line block of `love.graphics.printf` calls mixed with score digits and entity draws.

`assets/Ball.lua` (78 lines) owns position/velocity (`Dx`, `Dy`), AABB collision in `collides(paddle)` — which also pushes the ball out of the paddle before returning true — and wall bouncing in `update` (clamping y to the 2px margin and playing `wall_hit` from a sound loaded inside the Ball itself).

`assets/Paddle.lua` (28 lines) is a rectangle with `PADDLE_SPEED = 200`, driven by `love.keyboard.isDown` on constructor-provided key names and clamped to the window edges.

### Stack

LÖVE 11.x (inferred from the API — no version is pinned anywhere), Lua, plus two vendored libraries: `libraries/push.lua` (Ulydev, virtual resolution) and `libraries/class.lua` (jonstoler, OOP), both attributed in comments at the top of each asset file.

## What works well

- The push-library virtual resolution makes the game resolution-independent and correctly handles `love.resize`.
- Collision detection returns push-out (`self.x = paddle.x - self.WIDTH`), which prevents the ball tunneling through a paddle.
- State machine in `GameBoard:keyPressed` is small and readable: three Enter branches, one per state, with serve/play/done transitions all visible in one place.
- Everything is delta-time based (`Dx * dt`), so gameplay speed doesn't depend on frame rate.

## What I'd change

- **Unbounded ball acceleration:** `Ball:invertDirection` does `self.Dx = self.Dx * -1.10` — 10% faster on every paddle hit with no cap, and re-rolls `Dy` to `math.random(10, 150)`. By point nine a rally can be physically unplayable, and there's no terminal-velocity clamp anywhere in `Ball.lua`.
- **`GameBoard:update` calls `self.updateScore(self)`** (dot, not colon) — it only works because `self` is passed explicitly, and it contradicts the colon-style calls everywhere else.
- `GameBoard.lua` mixes three jobs — state machine, input handling, and rendering — in 129 lines; `render()` alone interleaves five conditional text blocks with score drawing. Splitting render from update would make the states testable.
- Inconsistent sound ownership: `wall_hit` is loaded inside `Ball:init`, while `paddle_hit`/`score` live on `GameBoard`.
- Fragile module loading: `assets/*.lua` use `require '../libraries/class'` — a relative path that only resolves because of how LÖVE sets the working directory.

## Still outstanding

- No pause state (Escape exits immediately, even mid-rally) and no single-player/AI opponent.
- No high-score or win-count persistence; scores reset to zero after every match.
- No README (only `LICENSE`), no tests, no pinned LÖVE version, single commit.
- `winingPlayer` typo in `GameBoard.lua`, and `math.randomseed(os.time())` in `init` is a bare reseed.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
