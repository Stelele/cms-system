# class-booking-system

A booking calendar for evening programming lessons: one teacher, two UK-based students, a single 20:30 Africa/Harare slot Mon–Sat.

## What it does

Students log in with an emailed one-time code (or Google sign-in), browse a month calendar of available days, and book, reschedule or cancel a lesson. Each booked day gets a Google Meet link, and the student can subscribe to web-push reminders. The teacher gets an admin view (`frontend/src/views/AdminView.vue`, 442 lines) listing users, bookings and blocked days, plus an ICS feed (`backend/Endpoints/IcsEndpoint.cs`). Bookings can be exported/subscribed to a calendar, and the whole thing runs as a PWA with a service worker (`frontend/src/sw.ts`).

## How it works

Entry point is `backend/Host/Program.cs` (64 lines): it wires cookie authentication (30-day expiry, 401/403 instead of redirects), EF Core Sqlite, data-protection keys persisted next to the DB file, then maps endpoint groups — `MapAuth`, `MapGoogleLogin`, `MapSlots`, `MapBookings`, `MapPush`, `MapAdmin`, `MapIcs`.

The backend is Clean Architecture in five projects: `Domain` (234 lines — `Slot`, `Booking`, `BookingPolicy`, `LessonTime`, `BlockedDay`), `Application` (1,279 lines of command/query handlers), `Infrastructure` (4,438 lines), `Endpoints` (558 lines) and `Host`. Handlers receive `IAppDbContext`, `ICurrentUser`, `IMeetLinkProvider` and `IBookingNotifier`. `CreateBookingCommandHandler` validates against `BookingPolicy`, lazily creates the `Slot`, calls Google Calendar for a Meet link, and handles a lost unique-index race on `Slot.Date` by clearing the change tracker and retrying once.

Data lives in a single Sqlite file (`data/booking.db`) in WAL mode. `Infrastructure/Migrations` holds eight EF migrations. Backups go to Cloudflare R2 via `AWSSDK.S3` (`R2BackupService.cs`, 190 lines); `MigrateAndSeedAsync` restores from R2 *before* migrating, with a 30-second timeout, so a fresh container boots from the real DB rather than an empty one that the nightly backup would then overwrite. Reminders are computed by `ReminderService` (213 lines) and pushed through `Lib.Net.Http.WebPush`.

The frontend is Vue 3 + TypeScript + Nuxt UI on Vite 8, ~2,277 lines of source, with `vite-plugin-pwa` and Workbox for offline caching.

### Stack

- **C# / .NET 10 + EF Core 9 (Sqlite)** — API and domain logic.
- **Vue 3 + Nuxt UI 4 + Tailwind 4** — the calendar and admin UI.
- **Playwright** — one 456-line journey spec, 16 tests, spinning up both servers.
- **xUnit + Vitest** — 3,511 lines of backend tests, ~600 lines of frontend tests.
- **Pulumi (C#) + docker-compose** — droplet provisioning, GHCR images, nginx vhost.

## What works well

- Test coverage is real, not decorative: 25 backend test files covering booking lifecycle (377 lines), Google login flows (361), push notifications (310) and API-level tests via `ApiFactory`.
- The R2 restore path is unusually careful about ordering and timeouts, with comments explaining *why* restore precedes migration.
- The concurrent-booking race in `CreateBookingCommandHandler` is handled explicitly rather than left to a 500.
- Secrets in `.env.local` are gitignored and untracked (verified via `git ls-files`).

## What I'd change

- `frontend/src/views/AdminView.vue` is 442 lines with no co-located test — the largest untested surface in the repo.
- `R2BackupService.cs:114` has a bare `catch` that only releases a gate before rethrowing; acceptable, but the restore path has no integration test asserting a real R2 round-trip.
- `AppDbContext` runs `PRAGMA journal_mode=WAL` via `ExecuteSqlRawAsync` string SQL in `DependencyInjection.cs` — untested and invisible to EF.
- The whole deploy story (Pulumi, GHCR, certbot) lives in `infra/` with no CI test for the Pulumi program itself.

## Still outstanding

- No `grep` hits for TODO/FIXME/HACK anywhere in `backend`, `frontend/src`, `e2e/tests` or `infra` — the codebase is clean of markers, which means unfinished work is unwritten rather than flagged.
- Only one e2e spec (`e2e/tests/booking.spec.ts`); no e2e coverage of Google OAuth, disconnect/privacy flows, or the admin UI.
- The `E2E` environment leaks the latest login code at `/api/test/latest-code/{email}` — gated by `EnvironmentName == "E2E"` but present in the compiled host.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
