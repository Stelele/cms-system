# frappe-pesepay

A Frappe app that plugs the PesePay gateway into Frappe/ERPNext, so Zimbabwean payment methods (EcoCash, InnBucks, Omari, Zimswitch, Visa/Mastercard, PayGo) can be taken from checkout pages, Payment Requests and the POS terminal.

## What it does

- **Multi-instance settings**: `Pesepay Settings` is a named DocType (one record per gateway: `gateway_name`, `integration_key`, `encryption_key`, sandbox toggle, currency-map child table). Saving it auto-creates the `Payment Gateway`, per-company `Payment Gateway Account` and `Mode of Payment` rows so the "Pay with PesePay" button appears in POS (`pesepay_settings.py:on_update`).
- **Redirect flow**: `get_payment_url` logs an `Integration Request` (Queued) and hands back `./pesepay_redirect?token=…`; the website page `templates/pages/pesepay_redirect.py` calls the PesePay API, stores `reference_number`/`poll_url`, and sends the payer to PesePay's hosted page.
- **Seamless flow**: `make_seamless_payment` in `templates/pages/pesepay_checkout.py` initiates a payment server-side from card/mobile-money fields, including a `card_details` payload.
- **Result handling three ways**: a guest webhook `callback` (decrypts the payload with AES-256-CBC, flips the IR to Completed/Failed), a per-minute scheduler `poll_pending_payments` for IRs queued >60 s, and client polling endpoints `poll_payment_status` / `poll_payment_reference`.
- **POS integration**: `public/js/pesepay_pos.js` (419 lines, injected for Sales Invoice + POS Invoice) detects PesePay-gated modes of payment, opens a pay dialog, polls every 3 s, then submits the invoice through ERPNext's own flow.

## How it works

- `pesepay/hooks.py` wires `doctype_js`, `scheduler_events.all` and `required_apps = ["frappe", "payments"]`. `pesepay_connector.py` (376 lines) is a hand port of PesePay's C# SDK: a `_AesCbcPayloadCrypto` helper over `cryptography.hazmat`, a snake→camel JSON encoder mirroring `JsonSerializerOptions`, a static `METHOD_CODES` table (`("EcoCash","USD"): "PZW211"` etc.), and `_post`/`_get` over `frappe.utils.get_request_session`. `overrides/invoice.py` exposes the whitelisted state/confirm methods the POS dialog calls.

### Stack

Python/Frappe v16 with the `payments` app, `cryptography>=41` (declared in `pyproject.toml`), optional ERPNext integration, vanilla JS against Desk dialogs, MariaDB via Frappe ORM plus one raw SQL join in `get_pos_pesepay_mode_names`.

## What works well

- Security thinking is visible: the webhook rejects payloads without `payload`, `poll_payment_status` allowlists `api.pesepay.com`/`api.test.sandbox.pesepay.com` against SSRF, `poll_payment_reference` verifies the gateway resolves to `Pesepay Settings`, and `_validate_encryption_key` enforces exactly 32 UTF-8 bytes.
- `get_payment_url` is idempotent — it returns the existing IR for a pending reference rather than creating a duplicate.
- Errors are logged with `frappe.log_error` at every boundary instead of being swallowed silently.
- Real docs: README (225 lines), `docs/USER_GUIDE.md`, `docs/DEVELOPER.md`, a screenshot.

## What I'd change

- **`mark_pesepay_payment_confirmed` trusts the client.** It is a plain `@frappe.whitelist()` with no permission check and no server-side re-query of PesePay; the browser says "SUCCESS" and the IR becomes Completed, after which the POS submits the invoice. A caller can confirm an unpaid IR.
- **`_create_payment_entry` never creates a Payment Entry** (`pesepay_settings.py:~365`): despite its docstring it only `db_set`s `status = "Paid"` and reference fields on the Payment Request.
- **Guest `make_seamless_payment` accepts a client-supplied `amount`** and only falls back to the document total when amount is falsy — nothing reconciles the paid amount against the invoice.
- **The webhook scans every Queued/Authorized IR** (`callback`, ~20 lines of loop) to find a reference number instead of filtering on it — O(n) per webhook.
- **No tests at all**, yet `ci.yml:105` runs `bench run-tests --app pesepay`.

## Still outstanding

- No `tests/` directory anywhere: AES round-trip, camelCase encoding and webhook state transitions are all untested.
- `validate_minimum_transaction_amount` is an explicit `pass` placeholder (`pesepay_settings.py:117`).
- `pesepay_checkout.html`'s card fields go through the server unconditionally; there is no hosted-card-field option.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
