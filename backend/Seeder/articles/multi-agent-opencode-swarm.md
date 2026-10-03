# multi-agent-opencode-swarm

A suite of OpenCode skills and agent definitions that make a cheap model do the coding while an expensive model only plans, dispatches, and verifies. Six files total, all Markdown plus one JSON config — there is no executable code in the repo.

## What it does

Installed as two skills (`swarm`, `using-swarm`) plus two subagent definitions (`swarm-worker`, `swarm-verifier`) in `opencode.json`, it turns "swarm: refactor the auth module" into a proposal-then-dispatch flow: the orchestrator reads AGENTS.md files and the file tree, presents a spawn proposal listing each worker's scope and contracts, waits for approval, dispatches all workers in parallel, runs a verifier per worker and one contract cross-check, then either retries through a four-level escalation ladder or prints a "SWARM COMPLETE" summary. The `using-swarm` meta-skill makes swarm the default for any task spanning more than three files or more than one project layer.

## How it works

Everything is prompt-level, not code. `skills/swarm/SKILL.md` (321 lines) is the orchestrator's instruction manual: seven phases drawn as a Graphviz DOT digraph, scoping rules (1–5 files per worker, one layer per worker, never two workers on the same file), a four-level escalation table, an eleven-row anti-pattern table and a ten-item red-flag list. `skills/using-swarm/SKILL.md` (110 lines) is the meta-skill that forces the proposal question, with its own DOT decision graph and a cost-model ASCII diagram (≈10% of tokens on the expensive model, ≈90% on `deepseek-v4-flash`).

Two template files supply the actual dispatch payloads: `worker-prompt.md` builds a prompt with SUBAGENT-STOP, an AGENTS.md fragment, a file whitelist, an output contract and a fixed return format; `verifier-prompt.md` has Mode 1 (per-worker: completeness, accuracy, correctness, contract compliance — each YES/NO with evidence) and Mode 2 (contract cross-check: name/signature match, orphans, unmet needs). Both mandate an exact `PASS` or `FAIL — [reason]` verdict.

`config/agent-config.json` (18 lines) defines both subagents as `mode: subagent`, `hidden: true`, `model: opencode-go/deepseek-v4-flash`, with `steps: 30` for workers and `steps: 15` for verifiers, and inline system prompts.

### Stack

No package manifest exists — the "stack" is Markdown skills with YAML frontmatter (`name`/`description`), DOT graph blocks as workflow specs, and one JSON agent-config file consumed by OpenCode.

## What works well

- Escalation is genuinely cost-ordered: levels 1–2 re-dispatch `swarm-worker` with feedback or broader context, only level 3 and 4 hand work to the orchestrator, and a hard rule caps re-dispatch at twice before re-scoping.
- The verifier templates demand evidence per check, and the usage notes explicitly reject vague verdicts like "FAIL — bad code".
- Context economy is written into the protocol: send AGENTS.md fragments, not whole files; verifiers get only task + output.
- The DOT flowcharts match the prose phases — the diagram and the seven-phase list describe the same graph.

## What I'd change

- **Nothing enforces any of it.** There is no hook, script, or linter. Every gate — the HARD-GATE before auto-dispatch, "wait for ALL verifiers", "never two workers on the same file" — lives only in prompt text that a model may ignore.
- **The contract cross-check has no plumbing.** `agent-config.json`'s worker prompt (line 8) asks only for files/summary/warnings and never mentions contracts; contracts exist solely in `worker-prompt.md`, so unless the orchestrator pastes that template verbatim, there is nothing for Mode 2 to check.
- **Verdict formats disagree.** `agent-config.json` says return `'PASS'` or `'FAIL - [specific reason]'`; `verifier-prompt.md` says `PASS — all checks passed` with an em dash. A strict parser would break.
- **The expensive model is never configured.** Both agents are `deepseek-v4-flash`; "v4-pro" appears only in prose, so the orchestrator model is assumed, not defined.
- Single commit, no LICENSE, no CI.

## Still outstanding

- Zero tests and zero validation harness — nothing checks that a dispatched prompt matches the template.
- No example transcript or recorded swarm run in the repo.
- README's `/swarm` slash command and auto-trigger rules are undocumented in any OpenCode config file; wiring them up is left to the reader.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
