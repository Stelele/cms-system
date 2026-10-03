# frappe-self-host

An offline-first deployment kit ("BasaPOS") for running Frappe/ERPNext v16 with custom apps on a single server — Linux shell pipeline, plus a Windows WSL installer, a watchdog daemon and a downloader, all in one repo.

## What it does

- **Linux**: `scripts/deploy-all.sh` chains `setup.sh` (installs Docker) → `build.sh` (builds `basapos:16` from the `frappe_docker` submodule using `apps.json`) → `deploy.sh` → `verify.sh` → `create-site.sh`. `OFFLINE=true` in `.env` switches Traefik to self-signed HTTPS; online mode uses Let's Encrypt (`scripts/setup-ssl.sh`).
- **Windows**: `setup-gui/` WinForms installer stitches split distro parts (`PartStitcher`), verifies `SHA256SUMS`, runs `wsl --import`, generates a 16-char admin password into `C:\BasaPOS\config\credentials.txt`, registers an autostart task, and starts `keeper/` — a watchdog that respawns the WSL keepalive, probes the site, backs off on failure and `FailFast`s itself if the main loop stalls (`KeeperLoop.IsStale`, 180 s).
- **Downloader**: fetches the GitHub release, downloads the payload parts, verifies hashes, rebuilds the USB layout.
- **Appliance**: `appliance/overlay/usr/local/sbin/basapos-firstboot` is a sentinel-per-phase, crash-safe first boot (docker load → env → site creation) that converges on re-run after a power cut; `smoke-firstboot.sh` exercises it.

## How it works

Everything is driven by `.env` (`DOMAIN`, `OFFLINE`, `DB_PASSWORD`, `ADMIN_PASSWORD`). `scripts/gen-compose.sh` merges the submodule's `compose.yaml` with MariaDB/Redis/proxy overrides plus `overrides/compose.selfsigned.yaml`, and `deploy.sh` moves the result to `compose.custom.yaml`. Custom apps come from `apps.json` (erpnext + three Stelele apps on `version-16`). Site creation reads `apps.txt` from the backend container and passes `--install-app` flags.

### Stack

Bash (14 scripts, 668 lines) over Docker Compose and the `frappe_docker` submodule; C#/.NET WinForms for `setup-gui`, `keeper` and `downloader` (~2,400 lines); xUnit tests (`setup-gui.tests`, `keeper.tests` — 50 `[Fact]`s); GitHub Actions with four lanes: `lint` (shellcheck), `gui` (dotnet test + cross-publish), `distro` (content-hash cache on GHCR + firstboot smoke) and `windows-drill` (real install/uninstall e2e).

## What works well

- The failure-catalog docs (`docs/ops/drill-failure-catalog.md`, `windows-issues-catalog.md`) and dated design specs in `docs/superpowers/` document real operational reasoning.
- Crash-safety is taken seriously: firstboot sentinels, `Credentials.RestrictToAdmins` using SIDs, keeper crash-loop markers, hash-verified part stitching (tested against corruption).
- CI actually runs the Windows installer end to end rather than just compiling it.

## What I'd change

- **The README never says `git submodule update --init`.** `build.sh` and `gen-compose.sh` both `cd frappe_docker/`, which is an empty directory in a plain clone — the documented Quick Start fails at step 4.
- **Weak shipped defaults**: `.env.example` has `DB_PASSWORD=change_this_to_random_password` and `ADMIN_PASSWORD=admin`, and `create-site.sh` falls back to `--admin-password admin`.
- **Fixed sleeps instead of health checks**: `deploy.sh` does `sleep 15` then `sleep 10` to wait for DB/configurator, commented as a timeout workaround.
- **Duplicated config derivation**: `deploy.sh` and `gen-compose.sh` each re-derive `SITES_RULE` from `DOMAIN`, with "keep in sync" comments in both — a classic drift point.
- **`build.sh` passes `CACHE_BUST=$(date +%s)`**, so every image build invalidates the layer cache.

## Still outstanding

- No tests for the 14 shell scripts beyond shellcheck lint and the one firstboot smoke; `backup.sh`/`restore.sh`/`setup-cron.sh` are never exercised in CI.
- `setup.sh` pipes `curl get.docker.com | bash`, which needs internet even in an "offline-first" workflow.
- The Windows runbook and LAN-mode notes live only under `docs/ops/`; the Quick Start links to the runbook but the Linux section never mentions the `frappe_docker` submodule checkout.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
