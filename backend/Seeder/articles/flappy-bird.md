# flappy-bird

A Flappy Bird clone written in Lua for LÖVE2D. The window title is `Fifty Bird`.

## What it does

A 1280×720 window renders a 512×288 virtual viewport. The background and ground scroll continuously; a title screen leads to a 3-2-1 countdown, then play. `Space` flaps, pipes spawn from the right every two seconds with a randomised gap, passing one scores a point and plays a sound. Touching a pipe or the ground ends the run, plays explosion + hurt, and shows a score screen; `Enter` restarts, `Escape` quits. Music loops from load with no way to mute it.

## How it works

`main.lua` is the entry point. At require time it pulls in `StateMachine` and the four states, defines `love.load` (fonts, `push:setupScreen`, `gStateMachine`, `gSounds`), `love.update` (scrolls background/ground, updates state machine, clears `keysPressed`), `love.draw` (draws background, delegates to `gStateMachine:render()`, draws ground, all inside `push:start()/finish()`), and `love.keypressed`.

`StateMachine.lua` (31 lines) is built on the vendored `libraries/class.lua`. It maps names → constructor functions (`'title'`, `'countdown'`, `'play'`, `'score'`); `change(stateName, enterParams)` calls `exit()` on the current state, constructs the new one, and calls `enter(enterParams)`. `states/BaseState.lua` is an empty class that the four states `:extend()`.

`objects/Bird.lua` holds `dy`, applies `GRAVITY = 20` per second, and on `wasPressed('space')` sets `dy = ANTI_GRAVITY = -5`. Its `collides(pipe)` does a box overlap inset by 2 px vertically and 4 px horizontally, using the `PIPE_WIDTH`/`PIPE_HEIGHT` globals. `objects/PipePair.lua` owns a `{upper, lower}` pair, moves both left at `PIPE_SPEED = 60`, and sets `remove` once off-screen; `objects/Pipe.lua` draws the top pipe vertically flipped. `states/PlayState.lua` owns the spawn timer, scoring, and the collision checks that trigger `gStateMachine:change('score', { score = ... })`.

### Stack

Lua (LuaJIT) via LÖVE. `.vscode/settings.json` configures the Lua language server for the LÖVE runtime. Two vendored libraries: `libraries/push.lua` (281 lines, virtual resolution) and `libraries/class.lua` (101 lines, prototype inheritance). Assets: four PNGs in `assets/`, two TTFs in `fonts/`, four WAVs plus a 1.5 MB `marios_way.mp3` in `sounds/`.

## What works well

- The state machine is textbook: every screen is its own file with `enter`/`exit`/`update`/`render`, and `BaseState` supplies no-op defaults so `StateMachine.change` can call them unconditionally.
- `love.keyboard.wasPressed` plus clearing `keysPressed` at the end of `love.update` gives edge-triggered input — a held key can't retrigger, and restarts are clean.
- `push`'s virtual resolution keeps the game at 512×288 regardless of window size, and `love.resize` forwards to `push:resize`, so it stays correct when maximised.
- Pipe Y positions are clamped relative to the previous spawn (`math.max(-PIPE_HEIGHT + 10, math.min(lastY + math.random(-20, 20), VIRTUAL_HEIGHT - 90 - PIPE_HEIGHT))`), so consecutive gaps can never drift somewhere unreachable.
- `Bird:collides` insets the hitbox by 2/4 px, which is a deliberate forgiveness margin rather than a naive rect overlap.

## What I'd change

- `Bird:update` mixes correct and incorrect integration: `self.dy = self.dy + GRAVITY * dt` but `self.y = self.y + self.dy` — position is **not** multiplied by `dt`. Fall speed therefore scales with frame rate; at 144 Hz the bird falls roughly 2.4× faster than at 60 Hz.
- `ANTI_GRAVITY = -5` is a flat velocity assignment rather than an impulse, so flap strength is frame-rate dependent too. There is also no ceiling check: `PlayState:update` only tests `self.bird.y > VIRTUAL_HEIGHT - 15`, so the bird can fly off the top of the screen and keep going.
- `PlayState:update` calls `table.remove(self.pipePairs, k)` while iterating the same table with `pairs`. Removing during traversal is undefined in Lua and can skip elements; it should iterate backwards with `ipairs` or collect indices first.
- `GAP_HEIGHT` is a file-level local in `PipePair.lua` that is read in `init` and then re-randomised at the end of `init` — a single mutable upvalue shared by every pair, not state on the object that owns the pipes.
- `Bird:collides` depends on `PIPE_WIDTH` and `PIPE_HEIGHT`, which are globals defined in `states/PlayState.lua`. `objects/Bird.lua` cannot be used without that state file.
- `Pipe:update` is an empty body; `PipePair:update` writes `pipe.x` directly on both children instead.
- Music autoplays on load with no volume, mute or toggle.

## Still outstanding

- One commit: `ff04c5d "added sounds"`. No README, no `conf.lua`, no tests.
- No pause state, and no best score: `ScoreState` only receives and prints the current run's score — nothing is written to `love.filesystem` between runs.
- No difficulty ramp — `PIPE_SPEED` is a constant, so the game plays identically at score 0 and score 100.
- The `.gitignore` is an unmodified LuaRocks template ignoring `*.src.rock`, `*.o`, `*.dll`, none of which this project produces.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
