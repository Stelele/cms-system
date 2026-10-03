# youtube-transcript-downloder-scripts

PowerShell scripts that bulk-download YouTube channel subtitles with yt-dlp and turn them into clean, timestamp-free `.txt` transcripts organised by channel.

## What it does

`get-transcripts.ps1 -ChannelName "MicroConf"` creates `output/MicroConf/`, locates (or downloads) `yt-dlp.exe`, converts any leftover `.vtt` files to `.txt`, runs yt-dlp against `https://www.youtube.com/@<channel>` with `--write-subs --write-auto-subs --sub-lang en --download-archive archive.txt --format "worst"`, converts the new `.vtt` files, deletes any `.mp4` that arrived, and prints a transcript count. `get-transcripts-batch.ps1 -ChannelsFile channels.txt` loops the same script over a `#`-commented channel list in a child `powershell -File` process and prints a Done/Failed summary with per-channel counts. `cleanup-vtt.ps1` (optionally `-ChannelName`) is the standalone converter for `.vtt` files left by earlier runs.

## How it works

Everything is in three scripts: `get-transcripts.ps1` (238 lines), `cleanup-vtt.ps1` (98), `get-transcripts-batch.ps1` (77). `Get-YtDlp` (lines 11–76) resolves the binary from the script directory, then `$env:PATH`, downloads the latest release from GitHub if missing, re-downloads if `--version` fails, and compares against `api.github.com/repos/yt-dlp/yt-dlp/releases/latest` to self-update — that API failure is caught and downgraded to a warning.

The cleaning core is a hand-rolled line filter: drop `WEBVTT`, cue indices (`^\d+$`), timestamp ranges, `Kind/Language/Note/Style/Region` headers; strip `<...>` inline tags; trim; keep a line only if it differs from the previous one; collapse blank runs. Files whose existing `.txt` still contains `<00:00:00.000>` tags are detected as "corrupted" and re-converted. Video IDs are recovered from the `[XXXXXXXXXXX]` filename pattern, used to rebuild `archive.txt` from transcripts that predate it, and appended (`youtube <id>`) after each successful conversion.

### Stack

Windows PowerShell only — no `package.json`, no module manifest; the README's prerequisite list is "PowerShell (Windows)". External dependencies are `yt-dlp.exe` (auto-downloaded from GitHub releases), the GitHub REST API for version checks, and YouTube itself. Output is UTF-8 text under `output/<Channel>/` next to `archive.txt`.

## What works well

- Idempotency is thought through: archive reconstruction from existing transcripts (lines 103–106), corrupted-transcript detection, and orphan-VTT cleanup mean re-runs are safe.
- `Get-YtDlp` handles missing, corrupt and stale yt-dlp without user intervention, and every failure path ends in a message naming the manual download URL.
- The README documents the pipeline, the exact output tree and a troubleshooting-ish script table.

## What I'd change

- `get-transcripts.ps1` never passes `--skip-download`, so it downloads a full (worst-quality) video for every entry and then deletes `.mp4`s at lines 229–235 — slow and bandwidth-wasteful for a subtitles-only job.
- The 60-line VTT conversion loop is duplicated verbatim inside `get-transcripts.ps1` (lines 109–167 and 173–227) and a third time in `cleanup-vtt.ps1`; a shared `Convert-VttToText` function would remove ~120 lines.
- The `$maxLines` guard `continue`s the *inner* `foreach ($line)` loop (lines 139–143, 194–198, cleanup 61–67), so "SKIPPED" is printed once per remaining line and the file is converted anyway — the intended abort never happens.
- yt-dlp's exit code is never checked (`$null = & $ytDlpPath ... 2>$null`, line 170), and stderr is discarded, so rate-limiting or a dead channel still reports "Done".
- Only `--sub-lang en` is requested, and channels with no English captions produce zero output with no explanation.

## Still outstanding

- No tests, no CI, no `Pester` specs; the repo's single commit is `18cc4ce "removed second channels test file"`.
- `channels.txt` ships with three real channel names baked in rather than an example file.
- The README's output tree promises `video-ids.txt`, but no script writes it — only `archive.txt` is ever created.
- The sanitising regex differs between scripts (`[<>:`"/\|?*!]` vs `[<>:`"/\\|?*!]`), so directory names can disagree between `get-transcripts.ps1` and `get-transcripts-batch.ps1`.
- `$ChannelName` is used raw in the URL, so passing `@Name` yields `youtube.com/@@Name`.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
