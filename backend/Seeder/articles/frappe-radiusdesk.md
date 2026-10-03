# frappe-radiusdesk

A Frappe app that talks to the RadiusDesk (cake4) RADIUS API to sell and generate internet vouchers from ERPNext — POS sales, web checkout, and captive-portal hotspot delivery for ISP/cafe deployments.

## What it does

- **Auto-vouchers on submit**: `hooks.py` `doc_events` runs `voucher_automation.create_vouchers_for_invoice` on `POS Invoice` and `Sales Invoice` `on_submit`, so a line item that maps to a `Voucher Plan` produces a voucher without a separate button.
- **POS sale flow**: `create_voucher_sale` opens a Draft `Voucher Sale` (idempotent per invoice), `initiate_voucher_payment` starts a PesaPay seamless payment (EcoCash/InnBucks/Omari), then the cashier confirms.
- **Cash at the till**: `confirm_voucher_sale` fulfils a Draft sale immediately for non-PesaPay modes.
- **Guest self-service**: `create_web_checkout` (rate-limited: 3/hour per phone, 60/hour per IP) returns an unguessable `checkout_token`; the public `/voucher-checkout/` page polls `confirm_voucher_web_checkout`, which re-polls PesaPay's `poll_url` before fulfilling.
- **Fulfilment**: `fulfill_voucher_sale` creates the RadiusDesk voucher, then a POS/Sales Invoice; a cron scheduler every 5 minutes re-runs `retry_fulfillment_failed_sales` and emails operators.

## How it works

Entry points are `radius_desk/hooks.py` (`required_apps = ["frappe", "erpnext", "payments", "pesepay"]`, `doc_events`, `scheduler_events`, `after_migrate`). The core is `radius_desk/doctype/voucher_sale/voucher_sale.py` (768 lines): the `VoucherSale` document plus the whitelisted API. Helpers live in `radius_desk/utils/` — `radiusdesk.py` (127 lines, HTTP connector), `voucher_automation.py`, `hotspot_embed.py`, `fulfillment_retry.py`, `system_user.py`, `pos_infra.py`.

Data model: `Voucher Plan` (plan, item, price, RadiusDesk realm/profile IDs) and `Voucher Sale` (amount, status, phone, `merchant_reference`, `checkout_token`, `voucher_code`, linked invoice). Fulfilment swaps `frappe.session.user` to an app-owned system user so ERPNext's permission-checked account lookup succeeds without using Administrator.

### Stack

Python/Frappe v16 on ERPNext with the `payments` and `pesepay` apps; vanilla JS for the guest page (`www/voucher-checkout/index.js`) and POS receipt widget; MariaDB via Frappe ORM.

## What works well

- **Test coverage is real**: 7 files under `radius_desk/tests/` — 75 test functions, 243 asserts — covering the connector, fulfilment retry, POS end-to-end flow and the voucher-sale state machine.
- **Hotspot URL handling is strict**: `validate_hotspot_url` in `hotspot_embed.py` accepts only IP-literal private/loopback/link-local hosts, no userinfo, and a conservative charset, with an exact-pin production match — it fails closed against open redirects and LAN injection.
- **Idempotency is deliberate**: per-invoice dedup in `create_voucher_sale`, `find_voucher_by_extra_value` before creating a voucher (RadiusDesk's add endpoint is not idempotent), and resumable fulfilment that survives a failed invoice step.
- Design specs under `docs/superpowers/` record the reasoning behind each feature.

## What I'd change

- **`confirm_voucher_sale` and `fulfill_voucher_sale` open with `frappe.get_doc(..., ignore_permissions=True)`** and have no role gate: any authenticated user who can reach the endpoint can fulfil a Draft sale and collect a voucher code without paying, bypassing the DocType permissions (System Manager full, Sales User read-only).
- **The POS confirmation trusts the client.** `confirm_voucher_payment` → `_confirm_voucher_payment` marks the Integration Request Completed based on the browser's SUCCESS claim; only the guest path (`confirm_voucher_web_checkout`) re-polls PesaPay's own `poll_url` before fulfilling.
- **`ensure_rate_limit` fails open by design** — a cache outage logs and allows unlimited guest checkout attempts, so the anti-bomb control silently disappears when Redis is down.
- **No CI**: there is no `.github/` directory, so ~2,000 lines of tests never run automatically.

## Still outstanding

- `_validate_pesepay_combo` only rejects unsupported method/currency pairs when `METHOD_CODES` imports successfully — an ImportError silently skips the check.
- `_client_ip` relies on the first `X-Forwarded-For` entry, acknowledged in the docstring as spoofable on a bare app server.
- The client side is uncovered: no `package.json`, no `.test.js`/`.spec.js` — `www/voucher-checkout/index.js` (224 lines) and `public/js/pos_voucher_summary.js` (212 lines) are untested; `test_embed_page.py` only asserts server-rendered HTML.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
