# fountain-of-life

A Vue 3 + Nuxt UI church site for Fountain of Life Family in Christ (Bulawayo, Zimbabwe) — an installable PWA with a YouTube sermon feed.

## What it does

Four routes: `/` (hero, service times, visitor modal, gatherings, testimonials, give, CTA, footer), `/watch` (sermons from two channels, modal player, "Load More"), `/connect` (social cards), `/about` (Leaflet map at `CHURCH_LAT/LNG`, beliefs accordion). The visitor card builds a WhatsApp `wa.me` deep-link. Fixed mobile tab bar, page fades, offline banner, install prompt and skip-to-content link.

## How it works

`index.html` inlines three scripts: force light mode into `localStorage`, stash `beforeinstallprompt` on `window.__pwaDeferredPrompt`, re-dispatch it as `pwa-installable`, and hand-register `/sw.js` with a `controllerchange` reload (VitePWA: `injectRegister: false`). `src/main.ts` mounts Vue with the Nuxt UI plugin and router (lazy imports, catch-all redirect to `/`).

- `src/data/churchInfo.ts` — every constant (address, phone, social URLs, coordinates, service times, channel IDs).
- `src/composables/useYouTube.ts` (165 lines) — rewrites `UC…` → `UU…` for `playlistItems` URLs, fetches both channels in parallel, keeps per-channel `ChannelState`, then `mergeAndSort` dedupes by id and sorts by `publishedAt`.
- `src/services/db/index.ts` + `videoCache.ts` — Dexie (`fountain_db`, `videoCache`) with a 1-hour TTL (`getCached/setCached/appendCached`); failures become `null`/no-op.

`useNewVideosAlert.ts` fetches 1 item per channel and diffs against `localStorage['fountain_last_seen_video_id']`. `useOffline` and `usePwaInstall` wrap `navigator.onLine` and the deferred prompt.

`vite.config.ts` sets Workbox caching: `i.ytimg.com` → `StaleWhileRevalidate` (7 days), `googleapis.com/youtube` → `NetworkFirst` (1 day), plus `public/manifest.webmanifest`. `.github/workflows/deploy.yml` builds `master` with `VITE_YOUTUBE_API_KEY`, copies `dist/index.html` to `404.html` for SPA routing, and force-pushes `dist/` to a build repo via `secrets.PAT`.

### Stack

Vue 3.5, vue-router 5, Nuxt UI 4.10, Dexie 4.4, Leaflet 1.9, vite-plugin-pwa 1.3, Vite 8, TypeScript 6, oxlint + ESLint + Prettier. 1,684 lines across 46 files.

## What works well

- Caching is layered: a 1-hour IndexedDB cache in `videoCache.ts` plus Workbox caches, so `/watch` renders offline instead of a spinner forever.
- `useYouTube` models pagination as per-channel state rather than one merged array, so "Load More" advances both channels independently before re-merging.
- Accessibility was a real task: `App.vue` has the skip link, `VideoCard.vue` carries `role="button"`, `tabindex="0"`, `aria-label` and Enter/Space handlers, `SiteNav.vue` labels its landmarks and pads the tab bar with `env(safe-area-inset-bottom)`.
- Storage access is guarded (`safeGetItem`/`safeSetItem`, try/catch in every Dexie helper), so Safari private-mode failures degrade rather than crash.
- `LocationMap.vue` deletes Leaflet's `_getIconUrl` and merges bundled marker icons — the classic bundler breakage — rather than broken markers.

## What I'd change

- **The YouTube API key ships to the browser.** `VITE_YOUTUBE_API_KEY` is read in `useYouTube.ts` and `useNewVideosAlert.ts`; Vite inlines `VITE_*` vars into the public bundle, so the key is plain text in the JS. `deploy.yml` passes it as `${{ secrets.VITE_YOUTUBE_API_KEY }}`, which makes it look protected when it isn't. This needs a small proxy endpoint or an HTTP-referrer-restricted key with a quota alert.
- `README.md` is still the untouched `npm create vue` template — "This template should help get you started developing with Vue 3 in Vite" — with no mention of the site, the required env var, or deployment.
- The "new video" alert can effectively never be seen. `WatchView.vue` calls `useNewVideosAlert()` (registering its `onMounted` first), then registers its own `onMounted` that awaits `fetchVideos()` and calls `markLatestSeen(...)`, which sets `newVideoCount.value = 0`. The only page that renders the banner clears it during the same mount it's checked in.
- `fetchVideos` returns silently when `API_KEY` is empty (`if (loaded || !API_KEY) return`) with `error` still `null`, so `VideoGrid` shows its "Check back soon for new videos" empty state — a missing key is indistinguishable from an empty channel.
- Dead config: `VITE_YOUTUBE_CHANNEL_ID` is in `.env.example` and passed as a build secret in `deploy.yml`, but `grep` finds no `import.meta.env.VITE_YOUTUBE_CHANNEL_ID` in `src/` (channels are hardcoded in `churchInfo.ts`). Likewise `ChurchInfo` in `src/types/index.ts` is implemented by nothing.
- Naming lies: `FacebookFeed.vue` contains no feed (four static `SocialLinkCard`s), and `NewsletterSection.vue` is a WhatsApp CTA with no newsletter form.

## Still outstanding

- `docs/superpowers/plans/2026-07-27-ui-polish-plan.md` Tasks 1, 2 and 10 were never done: there is no `src/app.config.ts`, no `src/components/LogoIcon.vue` (the logo is still an inline `<img>` in `HeroSection.vue` and `SiteNav.vue`), and `AppFooter.vue` is a hand-rolled `<footer>` rather than `UFooter`. Every checkbox in both plan files is unticked even though most steps *were* implemented.
- No tests of any kind — `package.json` has no `test` script and there are no spec files.
- `EventsSection.vue` and `TestimonialsSection.vue` hardcode their data as component-local arrays; there's no CMS or admin path to update them.
- `deploy.yml` uses `npm install` rather than `npm ci` and declares no `permissions:` block.
- The `/sw.js` registration in `index.html` has a `.then` with no `.catch`.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
