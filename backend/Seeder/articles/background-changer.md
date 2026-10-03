# background-changer

A Flutter Android app that sets a new wallpaper once a day from the same four sources as KDE Plasma's Picture of the Day plugin: Simon Stålenhag, Bing, NASA APOD and Wikimedia Commons — scheduled in the background, no ads.

## What it does

Pick a source, fit mode (centerCrop / asIs / blurPad), which screens (home, lock) and a Wi-Fi-only constraint on the single settings screen. A 24-hour WorkManager periodic task refreshes the wallpaper headlessly; opening the app also refreshes if today's wallpaper is missing or from a previous day. Applied images are kept as JPEG + JSON sidecar pairs in a history directory (trimmed to 14), rendered as a list with title, author, source link and timestamp. If the current image is older than three days, a system notification says the provider looks stale.

## How it works

`PotdProvider` (`lib/providers/provider.dart`) is the seam: `StalenhagProvider` scrapes `simonstalenhag.se` for `4k/…_big.jpg` links and picks by day-of-year index, `BingProvider`, `ApodProvider` and `WikimediaProvider` hit their JSON/HTML endpoints, all routed through `provider_http.providerGet`, which maps timeout/`ClientException`/`IOException` onto `PotdException` so callers only ever catch one type. `PotdService.run` serialises concurrent triggers through a Future gate, dedupes on `imageUrl`, decodes and transforms via `ImageFitter` (EXIF bake, square centre-crop, blur pad), then calls `WallpaperApi.setWallpaper`. `WallpaperRepo` owns `settings.json`, `current.json` (written via tmp file + rename) and `history/`, shared between the UI isolate and the headless one.

Native work lives in the local `packages/wallpaper_plugin`: `WallpaperPlugin.kt` (196 lines) answers `screenSize` (API 30 `maximumWindowMetrics` with two fallbacks), `set` (single-thread executor, per-flag fallback if the combined `setBitmap` fails), `notifyStale` and `requestNotifPermission`. `lib/background.dart`'s `callbackDispatcher` is the `@pragma('vm:entry-point')` entry WorkManager spins up.

### Stack

Flutter/Dart 3.13, `workmanager`, `http`, `image`, `path_provider`; Kotlin Android plugin exposing a raw `MethodChannel('wallpaper_changer/wallpaper')`; GitHub Actions `verify.yml` (analyze + test + debug APK) and `release.yml` (keystore from repo secrets, signed release APK).

## What works well

- 93 tests across 11 files, each provider with committed HTML/JSON fixtures, `DateTime now` injected for determinism, and `flutter analyze` on both app and plugin in CI.
- Error discipline: `PotdService._fitAndEncode` funnels decoder `Error`s into `PotdException` so `_run`'s `on Exception` boundary holds; `providerGet` documents the same contract.
- `ImageFitter._centerCrop` crops a square of `max(tw,th)` with an upscale cap of 2.5× — orientation-agnostic, so rotation never yields black bars.
- `settings.json` is written atomically and history trimming deletes orphaned `.jpg`s; gitignore keeps `build/` and `.dart_tool/` out of the repo.

## What I'd change

- **`apod.dart:14` hardcodes `api_key=DEMO_KEY`** — NASA's shared rate-limited key (≈30 requests/hour per IP), not configurable from settings, so APOD breaks for anyone else's traffic.
- The stale notification in `WallpaperPlugin.kt::postStaleNotification` has **no `PendingIntent`/`contentIntent`**: the text says "open the app", but tapping the notification does nothing.
- `callbackDispatcher` swallows everything with `catch (_) { return false }` (background.dart:20–22) — no logging, so a failed background run is indistinguishable from a benign skip until the 3-day stale notice.
- No tests for `lib/background.dart`, `main.dart` task registration, or the 196-line Kotlin plugin (no `androidTest/`); `home_screen_test.dart` has 3 tests for a 215-line widget.

## Still outstanding

- User-supplied NASA API key (none exists in `Settings` or the UI).
- Notification tap-through intent.
- Coverage for the headless entry point and native plugin.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
