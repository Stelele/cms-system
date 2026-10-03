# awesome-dashboard-scripts

A Frappe/ERPNext app that exposes the server side of a unified company dashboard: period metrics, charts, expense breakdowns, stock levels, item creation, and a one-call purchase cycle (PO → PR → PI → Payment Entry).

## What it does

Twenty-one `@frappe.whitelist(allow_guest=False)` methods under `awesome_dashboard/api/` return everything a dashboard needs for a company and date range: `dashboard_complete` builds current-vs-previous rows for sales, purchases, expenses, gross/net profit and margins; `dashboard_bar_chart` buckets POS Invoice sales by day/week/month/quarter; `grouped_expenses_summary` walks the Account tree via `lft`/`rgt` bounds to sum Direct + Indirect Expenses while excluding Stock Expenses; `get_stock_levels` joins `Bin`, `Item`, price lists and outstanding POS/Packed Item quantities. `create_full_purchase` submits a Purchase Order, Purchase Receipt, Purchase Invoice and a Payment Entry in one call, and `cancel_full_purchase`/`amend_full_purchase` reverse or rebuild that chain.

## How it works

`hooks.py` is almost entirely scaffold: the active lines are app metadata, a Role fixture for "Awesome Dashboard User", `required_apps = ["erpnext"]`, and `after_install`/`after_migrate` pointing at `installer.py`. `ensure_dashboard_permissions()` creates the role if missing and grants `DASHBOARD_PERMISSIONS` (16 ERPNext doctypes) through `frappe.permissions.add_permission` — its comment explains this deliberately avoids Custom DocPerm fixtures, which would *replace* a doctype's standard permission set. Every API module is re-exported from `api/__init__.py`; shared logic lives in `_utils.sanitize_time_grouping`.

### Stack

Python 3.10+/Frappe v16 + ERPNext v16, raw parameterised SQL through `frappe.db.sql`, ruff/pre-commit, GitHub Actions that bootstrap a full bench **including ERPNext** and run `run-tests --app awesome_dashboard`.

## What works well

- **Input hygiene on interpolated SQL**: `sanitize_time_grouping` allowlists the `DATE_FORMAT` string before it is spliced into an f-string (dashboard.py:55, 84; stock.py:123), and `dashboard_bar_chart` maps `grouping` through a fixed `format_map` and throws on anything else (dashboard.py:399–411) rather than passing raw input through. Everything else is `%s`-bound.
- `create_full_purchase` wraps the four-document chain in `try / frappe.db.rollback() / raise` (purchase.py:236–250), so a mid-chain failure does not leave a stray PO.
- Validation is front-loaded and specific (`_validate_purchase_inputs` checks company, supplier, warehouse, each item's existence and qty/rate sign, and duplicate `bill_no`).
- `installer.py` documents *why* it avoids fixtures, which is the kind of comment that survives refactors.

## What I'd change

- **`cancel_full_purchase` has no rollback** (purchase.py:295–314): it cancels PE, PI, PR and PO sequentially and only then calls `frappe.db.commit()`. If `pi.cancel()` throws after the Payment Entry was cancelled, the caller gets an exception with a half-cancelled chain — the exact failure the create path defends against.
- `_create_payment_entry` hardcodes `mode_of_payment: "Cash"` (line 158) and `exchange_rate: 1` (line 172): every purchase is paid in cash regardless of company defaults, and multi-currency suppliers cannot be paid.
- `_update_item_prices` runs *after* the commit, outside the try (lines 252, 432), so a price failure cannot roll back with the chain and partially updated Item Prices persist.
- `amend_full_purchase` re-implements `_validate_purchase_inputs` inline (lines 337–372) instead of calling it — the copies have already drifted (its error text mentions `sell_rate`, the shared validator does not).
- `dashboard_complete` (lines 118–388, 271 lines) fires 12+ sequential queries and never validates `company`/dates, unlike `grouped_expenses_summary`.
- `search_suppliers(company, ...)` validates `company` then never uses it in the filter (lookup.py:7–20).

## Still outstanding

- **No frontend in this repo**: no `page/`, no `doctype/`, `public/` contains only `.gitkeep`, and `patches.txt` is empty. The README's "Home → Awesome Dashboard" walkthrough assumes a UI that is not here.
- Zero tests; CI installs ERPNext and runs `run-tests` against an app with no test files.
- No `TODO`/`FIXME` markers anywhere.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
