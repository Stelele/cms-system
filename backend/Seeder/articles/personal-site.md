# personal-site

My personal site: a Vue 3 single-page app backed by a small Go API that scrapes old blog posts, proxies a private CMS, and deploys itself to DigitalOcean and GitHub Pages.

## What it does

The site serves an overview page, a CV, project pages grouped into Game Dev / Graphics / Business Case, and blog posts from three sources: old Medium and Hashnode posts scraped from GitHub backups, and current posts held in a private CMS. The Go server exposes `/medium-posts` and `/hashnode-posts` (scraped HTML parsed into JSON), `/feed?url=…` (raw fetch of any URL), and `/cms/blogs` plus `/cms/blogs/{blogId}/posts` as an authenticated CMS proxy. Projects bypass the Go server and hit the CMS's anonymous `/public` endpoints directly.

## How it works

`backend/main.go` (122 lines) builds a `gorilla/mux` router on `localhost:3000` behind one middleware, `addResponseHeaders`, which checks the `origin` header against a hard-coded allowlist (`localhost:5173`, `giftmugweni.com`, `anglican.masvingo.org`) and returns 401 before the handler runs. `backend/feeds.go` (607 lines) is the bulk: `getCachedFeed` memoises Medium and Hashnode results in a `feedCache` guarded by `sync.RWMutex`; `fetchGitHubRepoPosts` lists files from `Stelele/medium-blogs-backup` and `hashnode-blog-backups` on the GitHub API, downloads each `.html` in parallel goroutines, and hands them to `parseMediumPost`/`parseHashnodePost`, which walk the DOM with `golang.org/x/net/html` matching brittle class names like `dt-published` and `e-content`. `backend/cms.go` (156 lines) gets an Auth0 client-credentials token from env vars, caches it behind a mutex, and `proxyCmsRequest` forwards GETs with `Authorization: Bearer`.

On the frontend, `src/main.ts` mounts Vue with Pinia, `vue-router`, `@unhead/vue` and the Nuxt UI Vue plugin. `src/routes/index.ts` lazy-loads every page and keeps per-path scroll positions in a `Map`. `src/services/cms/index.ts` wraps `openapi-fetch` against a generated 837-line `schema.ts`; `src/services/public-cms/index.ts` is a separate tokenless client for projects, and `src/helpers/blogs/{medium,hashnode,cms}.ts` map each backend payload into a common `Blog`/`Post` shape.

### Stack

Go 1.25 with `gorilla/mux`, `golang.org/x/net/html` and `godotenv`. Vue 3.5, TypeScript, Vite 8, Nuxt UI v4, Tailwind 4, Pinia 3, `openapi-fetch` with generated types, `markdown-it`, `highlight.js`, `dompurify`, `plyr`. ESLint, Prettier and `vue-tsc` gate the build; `.github/workflows/deploy.yml` scp's the Go binary to a DigitalOcean droplet as a systemd service and pushes the built frontend to a `personal-site-build` repo.

## What works well

- The CMS client is properly typed: `openapi-fetch` against `schema.ts` means wrong paths and payloads fail `vue-tsc`, not runtime.
- `public-cms/index.ts` documents exactly why it exists and why it doesn't go through the Go proxy, including the env var split (`VITE_CMS_API_URL` vs `VITE_CMS_URL`).
- The GitHub-backup approach removes any live dependency on Medium or Hashnode; a parse failure on one file is logged and skipped rather than failing the batch.
- Route ordering in `routes/index.ts` carries an explicit comment about vue-router not backtracking once a param route matches.

## What I'd change

- **Zero tests in the entire repo** — no `*_test.go`, no `.spec.ts`. `parseMediumPost` and `parseHashnodePost` are pure functions over HTML fixtures and are the obvious first targets; `getCachedFeed`'s locking is the second.
- `frontend/.env` is tracked in git, and `deploy.yml` rebuilds it with `sed` in CI — the real config lives in two places.
- The feed caches in `feeds.go:33` have no TTL: `loaded bool` means a failed-then-succeeded fetch is frozen until the process restarts.
- `/feed?url=` fetches any URL the caller supplies (`feeds.go:145`) — an SSRF proxy with only the origin allowlist standing in front of it.
- `addResponseHeaders` 401s any request without an `origin` header (`main.go:96`), so curl, health checks and server-side callers are all rejected, and it injects `Content-Type: application/json` into every response including HTML.

## Still outstanding

- Dead code: `fetchAllPostsFromGitHub` (`feeds.go:182`) duplicates `fetchGitHubRepoPosts` and is never called; `renderNodeFrom` (`feeds.go:407`) is likewise unreferenced.
- `/blog` and `/books` routes render `WorkInProgress.vue` — both sections are placeholders.
- `helpers/blogs/cms.ts` fetches the blog list and then each blog's posts in a loop (N+1) and hard-codes `slugs: ["progamming", …]`, a typo that silently excludes the intended "programming" blog.
- ~30 MB of untracked build binaries sit in `backend/bin` and `backend/tmp`; `backend/tmp/air_errors.log` is tracked.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
