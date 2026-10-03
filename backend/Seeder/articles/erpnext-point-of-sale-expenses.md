# erpnext-point-of-sale-expenses

A Frappe/ERPNext app (`pos_expenses`) that adds expense posting, reprint and refund dialogs to the ERPNext Point of Sale page, so a cashier never leaves the terminal.

## What it does

- On the `point-of-sale` page, three extra buttons appear next to "New Invoice": **Add Expense**, **Reprint Invoices** and **Refund**.
- **Add Expense** opens a dialog (date, expense account, amount, remarks) and posts a two-line Journal Entry: debit the mapped expense account, credit the company's Cash account.
- **Reprint** lists submitted POS Invoices with date/time/status/search filters, shows item, tax and payment detail, and can print the full receipt or only ticked line items (rendered HTML cached in Redis for 10 minutes).
- **Refund** lists returnable invoices (checked with ERPNext's `is_invoice_returnable`), lets the cashier pick items and quantities, and submits a return POS Invoice — including partial returns with proportionally scaled payments.
- Closing the POS Closing Entry routes back to the POS page (`public/js/pos_closing_entry.js`).

## How it works

Entry point is `pos_expenses/hooks.py`, which injects `public/js/pos_extension.js` as `page_js` for the `point-of-sale` page. That 1012-line IIFE wraps `frappe.require` so that when `point-of-sale.bundle.js` loads it swaps `erpnext.PointOfSale.Controller` for a subclass that adds the three buttons and builds every dialog from concatenated HTML strings.

Server side, `pos_expenses/api.py` (529 lines) holds all whitelisted methods: `post_expense`, two tree-based account pickers (`pos_expense_account_query`, `indirect_expense_account_query` on `lft`/`rgt`), the reprint/refund invoice list and detail pairs, `process_pos_refund`, and `get_partial_print_url`/`show_partial_print`.

The data model is one DocType, `POS Expense Account` (`doctype/pos_expense_account/`): company, ledger account, friendly name, optional POS Profile, enabled flag. The controller enforces uniqueness per (company, account, profile), and queries give profile-specific rows priority over system defaults. `hooks.py` also overrides ERPNext's `get_parent_item_group` with `overrides/point_of_sale.py`, which computes the lowest common ancestor of the profile's item groups instead of upstream's arbitrary `list(set(...))` pick.

### Stack

Python 3.14 + Frappe v16 (ruff, pre-commit, GitHub Actions CI); vanilla JS against Frappe's Desk APIs — no build step, no framework; MariaDB via Frappe ORM plus three raw `frappe.db.sql` queries; Redis for the reprint cache.

## What works well

- `post_expense` checks Journal Entry permission, verifies the expense and cash accounts share a currency (`api.py:27-32`), and rejects unknown mappings.
- `process_pos_refund` (`api.py:372-464`) re-validates every line server-side against `get_invoice_item_returned_qty`, so the client cannot over-return.
- The item-group override documents the upstream bug it fixes and uses nested-set bounds correctly.
- Repo hygiene is real: pre-commit, eslint, two GitHub workflows, MIT license.

## What I'd change

- **No permission checks on the read/refund endpoints.** `get_pos_invoices_for_reprint`, `get_invoice_detail_for_refund` and `process_pos_refund` are plain `@frappe.whitelist()` — any logged-in user can list every POS Invoice and submit a return; only `post_expense` calls `frappe.has_permission`.
- **`post_expense` never validates `amount > 0`** and picks the cash account with a bare `{"account_type": "Cash", "company": ...}` lookup — with several cash accounts it silently takes an arbitrary one.
- **`pos_extension.js` is 1012 lines in one IIFE**, monkeypatching the global `frappe.require` and subclassing the ERPNext controller. An upstream change to `prepare_btns` or bundle loading breaks it; nothing guards a missing `erpnext.PointOfSale.Controller`.
- **Duplication:** `get_pos_invoices_for_reprint` and `get_pos_invoices_for_refund` (`api.py:172-312`) repeat the same filter-building block almost verbatim.
- **No tests anywhere** — no `tests/` directory exists, yet `.github/workflows/ci.yml:104` runs `bench run-tests --app pos_expenses`.

## Still outstanding

- Zero automated coverage of `api.py`, including the partial-return payment maths.
- `modules.txt` is empty, unlike a normal generated Frappe app.
- `README` claims CI runs unit tests on push to `develop`; there are none to run.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
