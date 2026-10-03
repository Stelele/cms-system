# erpnext-cashbook-reconciliation

A Node script that pulls new expenses out of a SQLite cashbook database and writes them as JSON, ready to be entered into an ERPNext instance as draft journal entries.

## What it does

Run `node main.js` (or `npm run main`) and it reads `./cashbookdb` read-only, selects expense rows from the configured cashbooks, normalises them, and writes `expenses_export.json` plus an incremental cursor in `expenses_export_config.json`. Rows already exported are skipped via `lastExportedTimestamp`. The script logs either `Exported N expenses to expenses_export.json` or `No new expenses to export`.

## How it works

Everything lives in `main.js` — 113 lines, CommonJS, no modules. It loads dotenv from `.env.local` then `.env`, reads `DB_PATH` (default `./cashbook.db`) and `BOOK_NAME` (a comma-separated list), then opens `sqlite3.Database(dbPath, OPEN_READONLY)`.

The query joins `entry` to `CashOutCategory` on `categoryId`, filters `bookname IN (…)` with bound placeholders, excludes `plusminus = 'false'` rows (i.e. keeps money-out) and `categoryName = 'Orders'`, and applies `e.last_edit_time > ?` with the stored timestamp. Results are then filtered again in JavaScript against `validExpenseTypes` (an 11-entry allowlist including both `Maintainace` and `Maintainance` spellings), mapped through `stripEmojis` (drops all non-ASCII, then trims) and `moment(e.date, "DD MMM YYYY").format("YYYY-MM-DD")`, sorted by date, and written with `JSON.stringify(..., null, 2)`. When any row is returned, the config is updated to `Math.max(...rows.map(r => r.last_edit_time))`.

The database itself has tables `entry`, `book`, `cash`, `CashInCategory`, `CashOutCategory`, `PaymentMode`, `Reminders`, `Note` — the schema of the cashbook Android app the data comes from.

### Stack

- **Node.js (CommonJS) + `sqlite3` 6** — read-only queries against the local SQLite file.
- **`moment` 2.30** — parsing the `"DD MMM YYYY"` dates and diffing for the sort.
- **`dotenv` 17** — config; no CLI argument parsing.
- **No test or lint tooling** — `package.json` has a single script, `main`.

## What works well

- The SQL uses bound parameters (`?` placeholders for both the book names and the timestamp), so `BOOK_NAME` values can't inject into the query.
- The DB is opened `OPEN_READONLY`, so a run can't corrupt the live cashbook.
- Incremental export via a persisted `lastExportedTimestamp` means repeated runs don't re-send history; the config file is two lines and human-readable.
- Emoji stripping (`stripEmojis`) is a real fix for category names coming off a mobile app, not a hypothetical.

## What I'd change

- **README is wrong about the core mechanism.** It says the script reads "the last exported entry ID" and queries `id > lastExportedId`, and shows an output shape without `id`/`bookname`. The code (`main.js`) uses `last_edit_time > ?` and emits both fields. `git log` shows the switch happened in the 2026-08-05 commit ("changed change tracking to timestamp based") and the README was never updated.
- **No tests at all** — `package.json` has no `test` script, no `tests/` directory, and no devDependencies. The date parsing, emoji stripping and allowlist filter are all unverified.
- `maxTimestamp` is computed from `rows`, not from the filtered `expenses`. If a batch's only new rows have categories outside `validExpenseTypes`, `expenses.length === 0`, the config is never advanced, and those rows are re-fetched on every future run forever.
- The connection error callback in `main.js` only `console.error`s and then logs "Connected…" regardless, so a bad `DB_PATH` still proceeds to the query. The `db.all` callback simply `throw err`, producing an unhandled stack trace.
- The allowlist contains both `Maintainace` and `Maintainance` — a data-quality problem papered over in application code rather than fixed upstream.

## Still outstanding

- The GitHub description says the output becomes "a draft journal entry into my erpnext instance", but there is no ERPNext/Frappe client here — the handoff is manual, driven by the JSON file.
- `expenses_export.json` is overwritten each run rather than archived, so nothing tracks what was actually sent.
- The 516 KB `cashbookdb` (plus `-shm`/`-wal`) sits in the working tree, gitignored by the `*db*` rule but not cleaned up; `.env` is tracked and lists real book names.
- No CI, no lint config, no `engines` field despite README claiming "Node.js v18+".

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
