# manim-animations

A uv-managed Manim project holding a single scene that animates a short lesson about what a function is — text, a box, arrows in and out, and shapes transforming into other shapes.

## What it does

`manim -pql main.py MainScene` renders the video — `manim.cfg` already pins `quality = low_quality`, and `.vscode/settings.json` sets Black as the format-on-save formatter. The scene plays through: a "Hello There" title, "Have you heard of functions", a rectangle drawn as "a box", arrows entering and leaving it, a loop of `Circle → Square`, `Triangle → Circle`, `Square → Triangle` transformations to show a pattern, and finally two `VGroup`s of three shapes each — inputs on the left, outputs on the right — with the box fading to a transparent outline and three arrows pointing into it.

## How it works

Everything is `main.py`, 94 lines, one class:

```python
config.media_width = "75%"

class MainScene(Scene):
    def construct(self):
        ...
```

`construct()` is a flat, top-down script of `self.play(...)` calls. There are no helper methods, no custom `Mobject` subclasses, no `always_redraw`/`ValueTracker` updaters, and no `self.next_section()` calls — just ten `Text` objects (`t1` … `t10`), one `Rectangle`, two `Arrow`s, and a handful of `Circle`/`Square`/`Triangle` mobjects.

The animation pattern throughout is mutate-then-move: an object is written (`Write`, `Create`), then slid with `t.animate.shift(UP*1.5)`, then replaced with `Transform(t3, tN)` — a single `t3` text mobject is reused as the running caption for the entire second half of the scene, so `t3` is retargeted at `t4`, `t5`, `t6`, `t7`, `t8`, `t9`, `t10` in sequence.

The final block builds `vg1` (circle/triangle/square stacked) on the input side and `vg2` (square/circle/triangle) on the output side, then does `Transform(vg1, vg2)` plus `rect1.animate.set_fill(opacity=0)` in a single `self.play`.

### Stack

- **Manim ≥ 0.19.0** (`pyproject.toml`), the community edition — `from manim import *`.
- **Python ≥ 3.13**, pinned by `.python-version` and `uv.lock`.
- **pyopengl + pyopengl-accelerate** for rendering, **ipython + notebook** for interactive use (hence `config.media_width = "75%"`, which only matters in a notebook).
- **`manim.cfg`** with a single setting: `[CLI] quality = low_quality`, so renders default to the lowest quality preset.

## What works well

- The scene is one readable, linear script. Because `construct()` never branches, you can scrub `main.py` top to bottom and know exactly what appears when — good properties for animation source that gets revisited and re-timed.
- `run_time=2` is passed explicitly on nearly every `self.play`, so pacing is deliberate rather than the default 1 second everywhere.
- The `Transform(t3, tN)` trick of keeping one caption mobject alive avoids the visual pop you get from `FadeOut`/`Write` pairs, and `VGroup(...).arrange_in_grid(rows=3)` in the last block keeps the input/output columns aligned without manual coordinates.
- The project is properly reproducible: `uv.lock` exists, `.python-version` pins 3.13, and `manim.cfg` pins render quality so two machines produce the same output.

## What I'd change

- **`README.md` is a 0-byte file.** There is no description of what the animations are, no render command, no note about `manim.cfg`'s quality setting — the entry point of the repo is empty.
- **`pyproject.toml` still carries uv's scaffolding defaults**: `name = "testing"`, `version = "0.1.0"`, `description = "Add your description here"`. The package is not identifiable from its manifest.
- **The whole scene is a single 94-line `construct()` with no sections or helper methods.** At ten `Text` objects and eight `Transform`s it is still followable, but there is no structure to grow into — adding a second lesson means either a second class or a 200-line method.
- **`rect1` is configured twice in a row** — `Rectangle().set_fill(WHITE, 0.8)` immediately followed by `rect1.fill_color = WHITE`, which discards the 0.8 alpha and is then animated to `opacity=0` at the end. One of the two assignments is redundant.
- **`config.media_width = "75%"` is set at module import**, i.e. it affects every render from this file, but it is a notebook-display setting and has no effect on CLI renders — dead configuration for the way the repo is actually run.

## Still outstanding

- No rendered output is committed: `.gitignore` ignores `media`, and no GIF/MP4 exists in the repo, so there is nothing to preview without running Manim locally.
- No tests, no lint config (no ruff/flake8, no Black config beyond the VS Code formatter setting), no CI.
- No second scene: `main.py` defines exactly one `Scene` subclass, and the repo description ("Storing all my manim animations") implies more were planned.
- Nothing is marked `TODO`/`FIXME`/`HACK`; there is no partial scene or commented-out block. The unfinished part of the repo is its packaging and documentation, not its code.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
