# godot-pong

Two-player Pong built in Godot 4.1 with GDScript. Player 1 moves with W/S, player 2 with the arrow keys, and the ball is served with the accept key.

## What it does

A 640×320 window (viewport stretch mode, fullscreen-capable) showing two paddles, two score labels and three helper-text labels. Pressing space serves the ball in a random direction; each paddle hit nudges the ball's vertical direction randomly. When the ball crosses either goal it stops, resets to the center, the scorer's label increments, both paddles snap back to their start positions, and the helper text reappears until the next serve. Scores are counted indefinitely — there is no win condition or reset.

## How it works

`project.godot` sets `scene.tscn` as the main scene. The scene is a `Node2D` root with `Paddle1`/`Paddle2` (instanced from `paddle.tscn`), `Wall1`/`Wall2`/`Gate1`/`Gate2` as `StaticBody2D`s, a `Ball` `CharacterBody2D`, a `CanvasLayer` holding five labels, and two `AudioStreamPlayer2D` nodes (`BallHit`, `Score`).

The logic lives in four scripts totaling ~148 lines:

- `scene.gd` (31 lines) holds `p1Score`/`p2Score`, updates the labels in `_on_ball_scored`, plays `Score`, and toggles the three helper labels in `_on_ball_is_playing`.
- `Ball.gd` (78 lines) does everything else: serving, movement, collision routing, scoring. It defines `scored` and `isPlaying` signals; `scene.tscn` wires `scored` to the root and both paddles (each paddle's `_on_ball_scored` resets it to `startPos`) and `isPlaying` to the root.
- `paddle1.gd` / `paddle2.gd` (20/19 lines) are near-identical `CharacterBody2D`s reading `Input.get_axis("p1_up", "p1_down")` at 300 px/s.

Collision handling is done by instance ID, not by node type: `Ball.gd:getIds()` walks `get_parent().get_node(...)` for all six static bodies, builds a dictionary, and `_process` compares `collision.get_collider_id()` against it every frame. Input actions (`p1_up`, `p1_down`, `p2_up`, `p2_down`) are declared in `project.godot`, so keys are remappable through the InputMap.

### Stack

Godot 4.1 (Forward Plus renderer), GDScript, and the committed `addons/godot-git-plugin` GDExtension for editor VCS integration. An export preset for the Web platform is configured in `export_presets.cfg`.

## What works well

- The signal wiring in `scene.tscn` keeps the ball ignorant of scoring UI and paddle resets — `Ball.gd` only emits, `scene.gd` and the paddles react.
- Input goes through named actions rather than hardcoded keycodes, so rebinding works without touching scripts.
- The whole game is under 150 lines of script with a clear per-node responsibility split.
- The Web export preset means the game was set up to ship in the browser, not just desktop.

## What I'd change

- **Ball speed is frame-rate dependent.** `Ball.gd:serve(delta)` sets `velocity = Vector2(xDir * randf_range(100, 200) * delta, ...)` — a per-frame motion value computed once — and then `_process` calls `move_and_collide(velocity)` every frame with that fixed value. On a 120 Hz display the ball moves twice as fast as on 60 Hz. The paddle-bounce branch has the same bug: `var y = randf_range(100, 200) * delta` re-derives a per-frame step from whatever `delta` happens to be that frame. Movement also runs in `_process` rather than `_physics_process`.
- **`getIds()` rebuilds six node lookups per frame** (Ball.gd lines 60–77) instead of using collision layers/groups or caching the IDs in `_ready`.
- `paddle1.gd` and `paddle2.gd` are duplicates that differ only in input action names; one script with an exported player index would do.
- **No win/reset state.** `scene.gd` increments scores forever; there is no target score, pause, or restart.
- Repo hygiene: two editor temp files (`sce23D0.tmp`, `sceDBCB.tmp`) and the git plugin's platform binaries (`.dll`, `.so`, `.dylib`) are committed, while `.gitignore` only excludes `.godot/`.

## Still outstanding

- No win condition, game-over screen, or score reset.
- No README and no tests (no test files or frameworks anywhere in the repo).
- Stray `.tmp` files and vendored plugin binaries still in the tree.
- No pause or settings: the game starts serving immediately and there is no way to restart a match without reloading.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
