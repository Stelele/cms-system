# awesome-butchery

A small ERPNext Point of Sale add-on: a per–POS Profile toggle that makes clicking an item open a NumberPad quantity dialog instead of dropping qty = 1 into the cart.

## What it does

Enable **Quick Quantity Dialog** on a POS Profile, reload the POS screen, and clicking an item opens a Frappe `Dialog` with ERPNext's `PointOfSale.NumberPad` attached. The OK button stays disabled until the entered quantity parses to a number > 0; Enter only submits when OK is enabled. If the item is already in the cart, the dialog pre-fills and edits that row's qty through `frappe.model.set_value` plus `cur_pos.update_cart_html(matchRow)` instead of adding a second line. Uncheck the box and the stock behaviour returns.

## How it works

There is no DocType and no Python request handler — the whole feature is client-side. `awesome_butchery/hooks.py` injects `public/js/pos_quick_qty.js` (185 lines) and `public/css/pos_quick_qty.css` (25 lines) via `app_include_js`/`app_include_css`, i.e. on **every** desk page. The file is an IIFE whose `init()` polls for `erpnext.PointOfSale.ItemSelector`, then monkey-patches `ItemSelector.prototype.bind_events` behind a `_quickQtyPatched` flag (lines 167–175) so it can call `hookItemSelector`. That function unbinds the stock `.item-wrapper` click handler and reads `data-item-code`, `data-batch-no`, `data-uom`, `data-rate` off the DOM to rebuild the item, then matches it against `window.cur_pos.frm.doc.items` in `getExistingQty` to decide between "edit row" and "add new".

The setting itself is a Check custom field `show_quantity_dialog` on POS Profile, defined twice — `migrate.py::after_migrate` calls `create_custom_fields` and `frappe.clear_cache`, while `fixtures/custom_field.json` syncs the same field (README documents both). `hooks.py` also declares the fixture and `after_migrate`; the remaining ~250 lines of that file are the stock commented scaffold.

### Stack

Python 3.10+/Frappe v16 + ERPNext v16 (pyproject pins `frappe >=16.0.0,<=17.0.0`), jQuery-style desk JavaScript, ruff/eslint/prettier via pre-commit, GitHub Actions with MariaDB + Redis services.

## What works well

- The dialog logic is genuinely careful: `updateState()` gates OK on `parseFloat > 0`, Enter is intercepted at the wrapper (lines 118–125), and the numpad's `firstDigit` flag replaces the pre-filled qty instead of appending to it.
- Reuse of ERPNext's own `NumberPad` means keyboard and touch input behave exactly as they do in the stock POS.
- Docs are unusually complete for an app this size (README 193 lines, `docs/USER_GUIDE.md` 105, `docs/DEVELOPER.md` 205), including a troubleshooting table and cache-clearing caveats.

## What I'd change

- **Zero tests.** `find` returns no `test_*.py` anywhere in the repo, yet README and `.github/workflows/ci.yml` both tell you to run `bench --site test_site run-tests --app awesome_butchery`. The CI "Find tests" step is `grep -rn "def test" > /dev/null`, which matches only the workflow file itself.
- CI never installs ERPNext, so the app's entire behaviour (patching `erpnext.PointOfSale.ItemSelector`) is never exercised.
- `init()` ends with `setTimeout(init, 500)` (pos_quick_qty.js:181) and never stops — a permanent 500 ms poll running on every desk page because of the global `app_include_js`.
- `getExistingQty` compares `i.rate === parseFloat(item.rate)` (line 13): exact float equality against a string scraped from a DOM attribute, so a rounding difference silently falls back to "add a duplicate row".
- The `serial_no` attribute is read (line 140) and put into `item` but never considered when matching, so serial-tracked items can match the wrong row.

## Still outstanding

- Test suite referenced by docs and CI does not exist.
- `hooks.py` still has commented `before_uninstall`/`after_uninstall` hooks — nothing cleans up the custom field on removal.
- No version guard: the prototype patch breaks silently if ERPNext renames `.item-wrapper` or `bind_events`.
- No `TODO`/`FIXME` markers anywhere; the gaps are structural, not annotated.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
