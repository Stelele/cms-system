# Pause-Audio

A single-file .NET 10 console program that mutes every currently-playing Windows audio session at once, using NAudio's Core Audio API.

## What it does

Run the executable and it opens the default render device, walks its audio session list, and sets `SimpleAudioVolume.Mute = true` on every session whose state is `AudioSessionStateActive` — Spotify, a browser tab, a game, anything — then prints one line per muted session with its display name and PID and a total count:

```
Muted session: Spotify.exe (PID: 1234)
Muted 3 currently active audio sessions.
```

That is the whole surface. There is no un-mute, no toggle, no hotkey, no pause window, no configuration: one process, one pass over the sessions, exit.

## How it works

`Pause Audio/Program.cs` (33 lines) is a top-level-statement program — no `Main`, no classes. In order:

1. `new MMDeviceEnumerator()` then `GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)` picks the active output device.
2. `device.AudioSessionManager` gives the session collection; a `for` loop indexes `sessionManager.Sessions[i]` directly (indexer, not `IEnumerable`).
3. For each session with `State == AudioSessionStateActive`, it writes `session.SimpleAudioVolume.Mute = true` and increments `mutedCount`.
4. The whole body sits in one `try/catch (Exception ex)` that prints `"Error: " + ex.Message`.

Nothing is stored anywhere; state lives entirely in the OS session manager. There are no external services. `NAudio.CoreAudioApi.Interfaces` is imported but never used — no `IAudioSessionEventsHandler`, so the program never learns when a session starts or stops.

### Stack

- **.NET 10** (`TargetFramework: net10.0`), `ImplicitUsings` and `Nullable` both enabled — `Pause Audio/PauseAudio.csproj`.
- **NAudio 2.2.1** — the only `PackageReference`; `MMDeviceEnumerator`, `AudioSessionManager` and `SimpleAudioVolume` come from its `CoreAudioApi`.
- **`Pause Audio.slnx`** — the new XML solution format, one project reference.

## What works well

- The core audio path is correct and minimal: default device → session manager → per-session mute, which is exactly what the Core Audio API intends.
- `Nullable` and `ImplicitUsings` are on, so the 33 lines carry no boilerplate and no obvious nullability warnings.
- It reports what it did — per-session lines plus a count — instead of failing silently.

## What I'd change

- **It pauses nothing.** The name and the GitHub description promise detection and muting of "all programs playing audio", but there is no state saved for the muted sessions (`mutedCount` is the only bookkeeping), so nothing can ever be unmuted. At minimum keep a list of muted `IAudioSessionControl`s and offer an `--restore` path.
- It only sees sessions on the *default* render endpoint at the instant it runs; sessions on other devices, or opened a millisecond later, are untouched. No `IAudioSessionManager2` notification registration, so there is no "pause" in progress — just a snapshot mute.
- The single `catch (Exception)` at line 28 collapses a missing device, an `UnauthorizedAccessException` and a null session into `Console.WriteLine("Error: " + ex.Message)` with no exit code, so a script can't tell success from failure.
- `MMDeviceEnumerator` is never disposed, and `session.DisplayName` is printed unguarded even though it is frequently empty for UWP/Store apps — the log line then reads `Muted session:  (PID: …)`.

## Still outstanding

- No README — the repository contains only `Program.cs`, `PauseAudio.csproj`, `Pause Audio.slnx` and `LICENSE.txt` (an unfilled MIT template still reading `Copyright (c) [year] [fullname]`).
- No tests of any kind, and no test project in the solution.
- No restore/unmute path, no argument parsing (`args` is never read), no pause duration or hotkey handling — the "pause" half of the feature does not exist.
- Single commit ("Add project files."), so there is no visible iteration history.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
