# erpnext-dashboard

A business-intelligence dashboard for monitoring ERPNext metrics — sales, stock, expenses and site performance — with offline support, interactive charts and Excel export.

## What it does

You log in with email and password against the backend's `/auth/login`, pick a company, and browse views for overview metrics, expenses and stock. Data is read from and written to one or more ERPNext sites: purchases are raised as purchase order → receipt → invoice → payment entry (`PurchasePayload`/`PurchaseResult` in `ErpNextService.ts`), stock can be reconciled through `StockReconciliationModal.vue`, and expenses flow through `ExpenseForm`/`ExpenseTable`. Company settings hold chart colours, logos and theme mode; users and roles are managed per company.

## How it works

Entry point is `backend/Host/Program.cs` (51 lines): it configures OpenAPI with a document and nullable-schema transformer, calls `AddApi()` / `AddApplication()` / `AddInfrastructure()`, then builds the pipeline — `GlobalExceptionMiddleware`, `SecurityHeadersMiddleware`, CORS (`AllowFrontend`), rate limiter, authentication/authorization, and `UserContextMiddleware` — before mapping each layer's endpoints.

The backend is Clean Architecture in six projects: `Domain` (253 lines — `Site`, `Company`, `User`, `Session`, `CompanySettings`, `ExpenseType`), `Application` (1,836 lines of MediatR commands/queries with a FluentValidation `ValidationBehaviour`), `Infrastructure` (5,774 lines, dominated by EF migrations), `Endpoints` (761 lines — `AuthEndpoints`, `UsersEndpoints`, `CompanyEndpoints`, `ExpenseEndpoints`, `SitesEndpoints`) and `Host`. Auth is session-based via `Authentication/SessionAuthenticationHandler.cs` (50 lines) with scope and role checks in `ScopeHandler`/`RolePermissions`.

Data flows through two distinct paths. The dashboard's own state lives in EF Core Sqlite (`ConnectionStrings:Sqlite`, `Data Source=erpnext.db`), while live ERP data is fetched by the **browser** calling ERPNext directly: `AuthStore.loadSiteData` pulls `site.url` and `site.apiToken` from the backend, and `ErpNextService.ts` (649 lines) attaches them as `config.baseURL` and `Authorization: token ${siteToken}` on every axios request.

### Stack

- **C# / .NET 10 + EF Core 10 (Sqlite)** — API, session auth, domain and persistence.
- **Vue 3 + Vite + Nuxt UI 4 + Pinia** — SPA with `vite-plugin-pwa` and Dexie for offline caching; `openapi-typescript` generates `src/services/api/schema.ts` (1,273 lines).
- **Chart.js / vue-chartjs** — charts; **axios** for direct ERPNext calls; **zod** for validation.
- **Docker** — `backend/docker-compose.yml` runs the image with `ConnectionStrings__Sqlite=Data Source=/app/data/erpnext.db`.
- **GitHub Actions (`deploy.yml`)** — path-filtered build, `dotnet test`, Docker Hub push, SSH deploy.

## What works well

- Backend tests are substantive: 1,280 lines across `AuthTests` (321), `ExpenseTests` (193), `CachingTests` (156) plus companies/users/sites suites, with an `IntegrationTestFactory`, `TestAuthHandler` and `StubR2StorageService` so tests run without real credentials.
- CI actually runs them — `deploy.yml` executes `dotnet test --configuration Release --no-build` before the image push.
- The generated OpenAPI schema keeps the Vue client typed against the real API rather than hand-written DTOs.
- `docker-compose.yml` templates every secret as `{{PLACEHOLDER}}` and keeps the DB on a named volume, so nothing sensitive is baked into the image.

## What I'd change

- **`Site.ApiToken` is stored in plaintext and shipped to the browser.** `Domain/Sites/Site.cs:11` holds it as a plain string, `DTOs/SiteResponse.cs:10` returns it, `AuthStore.ts:72` stores it in a Pinia ref, and `ErpNextService.ts:78`/`:93` send it as `Authorization: token …`. Any XSS or anyone with the JWT reads every ERPNext token the account can see; proxying those calls through the backend would keep them server-side.
- **The README's Quick Start is wrong**: it documents PostgreSQL and a `backend/Env/` folder, but the provider is `Microsoft.EntityFrameworkCore.Sqlite`, the config key is `ConnectionStrings:Sqlite`, and the compose file is `backend/docker-compose.yml`. It also points at `backend/Api/Host`, which does not exist (`backend/Host`).
- **No frontend tests.** `package.json` scripts are `dev`/`build`/`codegen` only — no vitest — so 9,722 lines of Vue, including `PurchaseForm.vue` (542) and `StockReconciliationModal.vue` (435), are untested while CI happily passes.
- `frontend/src/components/PurchaseForm.vue` (542 lines) and `ExpensesView.vue` (431) are large single-file components with no extracted logic to test.

## Still outstanding

- No `TODO`/`FIXME`/`NotImplemented` markers anywhere in `backend/Application`, `Domain`, `Api` or `frontend/src` — unwritten work is unflagged rather than tracked.
- Frontend test harness (vitest + test script) and at least coverage for the purchase and stock-reconciliation flows.
- Fixing the README database/paths section so the documented Quick Start matches the code.
- `frontend/dev-dist/` (generated Workbox bundles, ~14k lines) sits untracked in the working tree.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
