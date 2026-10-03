# whatsapp-church-bus-bookings

A WhatsApp bot intended to collect booking details from church members who want a seat on the church bus for the upcoming service. The repository contains no code — it is scaffolding only.

## What it does

Nothing can be demonstrated from this repository. The only statement of behaviour is the two-line `README.md`: "A whatsapp bot that collects booking information about members that want to be on the church bus for the upcoming service." There is no entry point, no message handler, no booking data model, and no configuration for any WhatsApp provider (Twilio, Meta Cloud API, Baileys, whatsapp-web.js, etc.), so how a member would start a conversation, what fields would be collected, and where bookings would be stored are all undetermined by the source.

## How it works

It does not. The complete file list is:

| File | Lines | Content |
|---|---|---|
| `README.md` | 2 | Title + one-sentence description |
| `LICENSE` | 21 | MIT, "Copyright (c) 2024 Gift Mugweni" |
| `.gitignore` | 130 | The stock Node.js template (npm/pnpm/yarn logs, `node_modules`, `dist`) |

Git history is a single commit, `7a9674d "Initial commit"`, dated 2024-04-01, touching exactly those three files; there are no other branches and no deleted files hiding earlier work.

### Stack

None declared: no `package.json`, no `requirements.txt`, no lockfile, no source directory. The only signal about intended technology is the Node-flavoured `.gitignore`, which suggests JavaScript/TypeScript was planned rather than Python or Go.

## What works well

- The intent is documented in one plain sentence in the README, which is more than many empty repos manage.
- MIT licence present from the first commit, and `.gitignore` was laid down up front, so an accidental `node_modules` commit was never a risk.

## What I'd change

- There is nothing to change yet — the honest answer is that this is a placeholder, not a project. Any critique of architecture or tests would be invented.
- The README should at minimum record the decisions that were never made: which WhatsApp API is targeted, how the bot is triggered (keyword, menu, or webhook), and where bookings persist (spreadsheet, database, ERPNext, or just a group message).

## Still outstanding

- All of it. Concretely, the repo is missing: `package.json` and any dependency manifest; a webhook or polling entry point; conversation/state handling for a multi-step booking flow; storage for bookings; environment configuration (API keys, phone numbers); tests; and any CI. The single 2024-04-01 commit is the entire history, so no work was started and later removed.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
