# stock-reconciliation

A small Node script that reconciles end-of-day stock between an ERPNext site and the shop's day-end Excel sheet, then creates a draft POS Invoice for the difference.

## What it does

Run `node main.js` (or `npm run main`) and it: (1) calls `getPaidPOSInvoices()` and refuses to run if any Paid POS Invoice for the company exists, to avoid double-invoicing; (2) fetches every `Bin` row for warehouse `Stores - NEs` with non-zero quantity via `GET /api/v2/document/Bin`; (3) reads the latest worksheet of two day-end Excel files (Butchery and Liquor) built from a path template containing today's year/month; (4) matches Excel `item` codes to ERP `item_code` and computes `sold = actual_qty - Excel "end "`, printing the non-zero deltas as a `console.table`; (5) looks up `Standard Selling` prices and POSTs a draft POS Invoice with `update_stock: 1` and a single Cash payment line.

## How it works

Two files carry everything: `main.js` (137 lines) is orchestration and Excel handling; `erpnext.js` (191 lines) holds the four API functions (`getErpStockData`, `getItemPrices`, `enterSales`, `getPaidPOSInvoices`). `main.js` imports `dotenv` and loads `.env.local` then `.env`, then uses SheetJS (`xlsx`) to read the workbook: `fetchShopData()` takes the last `SheetNames` entry, hard-trims the first two rows by mutating `range.s.r = 2`, filters rows to those containing `add` and `total`, and sorts by `item`. `processData()` loops ERP rows and does a linear `.find()` per row against the shop rows, dropping any Excel item that has no ERP match. `enterSales()` builds the invoice body with hardcoded `company`, `customer`, `pos_profile`, `currency` and item `warehouse`, and `Math.ceil`s the summed payment total.

### Stack

Node 18+ ES modules (`"type": "module"`), native `fetch` against the ERPNext v2 document API, `dotenv` for `ERPNEXT_URL`/`ERPNEXT_TOKEN`, and SheetJS pinned to the SheetJS CDN tarball (`xlsx-0.20.3.tgz`) rather than npm. Git history shows it previously drove a headless browser: commits `6500d88`/`117063d` removed a Puppeteer flow in favour of pure API calls.

## What works well

- The unpaid-invoice guard in `main()` (lines 115–123) is a real safety interlock for a script that posts financial documents, printed as a loud banner before aborting.
- Config is genuinely env-driven for credentials: `.env`/`.env.*` are gitignored and not tracked, so no token is in history.
- `JSDoc` typedefs (`ShopData`, `ErpStockEntry`, `SoldItem`) document the row shapes and column names, including the trailing space in `'end '`.
- The README is unusually practical — it names the exact file and line to change for warehouse, price list and defaults.

## What I'd change

- `getItemPrices()` (erpnext.js:112–123) dereferences `price.price_list_rate` with no check; one item missing from the price list throws `TypeError: Cannot read properties of undefined` and aborts the whole run after the table was already printed.
- Every `fetch(...).then(res => res.json())` ignores `res.ok`. A 401 returns a body with no `data`, so `getErpStockData()` returns `undefined` and `processData(undefined, ...)` crashes — the failure mode the README tells you to debug as "401/403".
- `main.js:134` filters to `sold > 0`, so stock counted *higher* than ERP is silently discarded rather than reconciled.
- `enterSales()` logs `response.status` (erpnext.js:164) on the parsed JSON body, which is always `undefined`.
- Business identifiers are hardcoded throughout `erpnext.js` (warehouse `Stores - NEs`, company `Njeremoto Enterprises`, POS profile, USD), and the README points at line numbers that will drift.

## Still outstanding

- No tests at all: no test file, no `test` script in `package.json`, no CI config.
- `getShopDataFileName(dept = "Butchery" | "Liquor")` (`main.js:102`) uses a bitwise-OR expression as a default argument; it evaluates to `0`, not a string — harmless only because both call sites pass explicit arguments.
- No dry-run mode: the script POSTs an invoice as its only output path, with no preview/confirmation flag.
- Negative-variance and unmatched-Excel-item handling is absent rather than stubbed — those rows vanish without a log line.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
