# breakout

A repository placeholder for a Breakout clone. **There is no source code in this repo — it is a stub.**

## What it does

Nothing. The working tree contains two files and no executable entry point.

## How it works

Cannot be described from the code, because there is no code.

Concretely, the repository at `/tmp/opencode/repos/breakout` contains:

| Path | What it is |
|---|---|
| `LICENSE` | MIT, 21 lines |
| `.gitignore` | A generic Lua / LuaRocks template (ignores `luac.out`, `*.src.rock`, `*.o`, `*.so`, `*.dll`, …) |
| `.git/` | One commit: `c4977bd "Initial commit"`, which adds exactly those two files |

There is no `README`, no manifest (`package.json`, `.rockspec`, `conf.lua` — nothing), no `src/`, no `main.lua`, no assets, no tests, and no other branches on `origin/main`.

### Stack

Undetermined. The only signal is the `.gitignore`, which is the stock LuaRocks/Lua template and implies Lua — consistent with the GitHub description "a breakout clone" — but no Lua file was ever committed.

## What works well

Nothing can be assessed. The two files present are valid: `LICENSE` is a complete MIT licence, `.gitignore` is syntactically fine.

## What I'd change

- The repo needs at least an entry point and a README before it describes anything. As it stands the GitHub description "a breakout clone" is the only claim, and nothing in the repository supports it.
- The `.gitignore` is a copy-paste of a LuaRocks template that references build artefacts (`*.src.rock`, `luac.out`, `*.elf`) this project will never produce. If the game was ever started locally, it was never committed.

## Still outstanding

- **Everything.** The entire implementation is missing: no game loop, no ball, no paddle, no bricks, no collision, no rendering, no run instructions.
- No commit history beyond the initial scaffold — there is no evidence work began and was abandoned; the files were never added at all.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
