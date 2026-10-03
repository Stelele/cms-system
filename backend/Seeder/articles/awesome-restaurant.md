# awesome-restaurant

Restaurant-style POS table management for Frappe/ERPNext v16: a visual table grid, tips, kitchen order broadcasting, draft persistence, and locking an order once the kitchen marks it Ready.

## What it does

Opening the standard ERPNext Point of Sale for a profile with linked tables renders a `.pos-table-grid` of `.pos-table-card`s instead of a list. Free tables start an order, occupied ones restore the saved draft invoice. The cart gains an **Add Tip** block backed by a `custom_tip_item` Link on the POS Profile, a **Send to Kitchen** button that sets `kitchen_status = Received`, and — once kitchen marks it **Ready** — a lock that hides the item selector, disables the numpad and customer selection, and swaps in **Print Bill**. A separate `kitchen-display` Page polls `get_kitchen_orders()` every 30 s and reacts to `kitchen_order_update` realtime events.

## How it works

The Python surface is one file, `awesome_restaurant3/pos_table_utils.py` (190 lines): `broadcast_table_update` / `broadcast_kitchen_update` publish realtime events from `doc_events` hooks on POS Table and POS Invoice; `send_order_to_kitchen`, `mark_order_ready`, `get_kitchen_orders`, `get_tip_item_code` are whitelisted; `free_tables_if_all_sessions_closed` runs on POS Closing Entry submit. Schema comes from `hooks.py`: `page_js` injects `restaurant_pos.js` on `point-of-sale`, `app_include_css` loads `pos_table.css`/`kitchen_display.css`, `custom_fields` adds `restaurant_table`, `custom_tip_amount`, `kitchen_status`, `sent_to_kitchen_at` to POS Invoice (plus POS Invoice Item, Sales Invoice, POS Profile), and three `patches/v1_0/*.py` re-apply them on migrate. The DocType is `POS Table` with an `applicable_profiles` child table of `POS Table Profile`.

The client is where the weight sits. `restaurant_pos.js` (97 lines) save-and-delegates `frappe.pages["point-of-sale"].on_page_load`, temporarily intercepts `frappe.require("point-of-sale.bundle.js")` to chain-load `restaurant_pos.bundle.js`, polls every 50 ms for `pos.pos_profile`, then copies every method of `RestaurantPosController.prototype` onto the live POS instance. That class (902 lines) extends `erpnext.PointOfSale.Controller` and overrides `prepare_dom`, `make_app`, `init_item_cart`, `init_item_selector`, `init_payments`, plus tip UI, table grid, badge rendering and lock helpers.

### Stack

Python/Frappe v16 + ERPNext v16, jQuery desk JS with BEM CSS, ruff/eslint/prettier, GitHub Actions.

## What works well

- Feature breadth is real and documented: profile-scoped tables, tip UI, kitchen state machine, cross-app compatibility with `pos_expenses`/`awesome-butchery` — README is 217 lines plus `docs/DEVELOPER.md` (320) and dated superpowers specs/plans.
- `mark_order_ready` restricts to Kitchen User/System Manager and `broadcast_kitchen_update` guards on `has_value_changed`, so realtime traffic is minimal.
- Guard flags (`_restaurant_make_app_done`) and the `refresh` reset show the SPA-navigation failure modes were actually hit and handled.

## What I'd change

- **`send_order_to_kitchen` is whitelisted with only a Guest check** (pos_table_utils.py:63–90) and saves with `doc.save(ignore_permissions=True)` — any logged-in user can mutate any draft POS Invoice.
- Its sibling `mark_order_ready` allows the **Kitchen User** role, but the `kitchen-display` Page grants only **System Manager**, so a kitchen user can call the API yet never open the display (README states this openly).
- `restaurant_pos.js` monkey-patches `frappe.require` and `Controller.prototype.make_app` with a 50 ms poll — race-prone interop that breaks on any ERPNext refactor; `_fix_table_grid_css` deletes datepicker DOM nodes off `body`.
- `frappe.realtime.off("pos_table_update")` (controller, `load_table_grid`) removes *every* listener for that event, not just its own.

## Still outstanding

- `test_pos_table.py` is 0 bytes; the six Gherkin files in `testing/features/` have no step definitions and no runner; `cypress.config.js` targets `./cypress/integration/*.js` but **no `cypress/` directory exists**.
- CI's only discovery step is `grep -rn "def test"` and never installs ERPNext or the three sibling apps the README says are required, so `run-tests --app awesome_restaurant3` proves nothing.
- No `TODO`/`FIXME` markers; the unfinished work is the absent test harness.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
