# FirewallHackathon

The Android app Team Firewall built for the 2019 Allan Gray Hackathon — a single-commit project consisting of the Android login template with a Firebase Realtime Database write bolted onto the login path.

## What it does

- Launches straight into `ui.login.LoginActivity`, the stock Android form: username/password fields with live validation, a loading spinner, and a welcome toast on success.
- On every login attempt, `data/LoginDataSource.login()` writes `"Hello, World!"` to the `message` node of the Firebase Realtime Database, then returns a randomly generated `LoggedInUser(id, "Jane Doe")`.
- After a "successful" login the activity sets `RESULT_OK` and calls `finish()`, which closes the app — no second screen exists.

There is nothing else. `AndroidManifest.xml` declares exactly one activity (the launcher); no layouts beyond `activity_login.xml`.

## How it works

The code path is the Google login-template stack, unmodified: `LoginActivity` (131 lines) observes `LoginViewModel` (70 lines), which uses `LoginFormState` for field validation (password must be > 5 characters — `LoginViewModel.java:68`) and `LoginViewModelFactory` to build `data.LoginRepository`, which delegates to `LoginDataSource`.

`LoginDataSource` is the only file with original content: it calls `FirebaseDatabase.getInstance()`, gets a reference to `"message"`, calls `setValue("Hello, World!")`, ignores the write result and any error callback, and falls through to a hardcoded fake user. The listener that follows (`addValueEventListener`) reads a snapshot into an unused local and is otherwise dead code.

`app/google-services.json` supplies the Firebase project (`firewallhackathon-3587c`) and API key.

### Stack

Java on Android (compileSdk 29, AGP 3.5.1, minSdk 15, `jcenter()` repositories), AndroidX appcompat/material/lifecycle, `com.google.firebase:firebase-database:19.1.0` with the `google-services` plugin, JUnit 4 + Espresso declared as test dependencies.

## What works well

- The login form validation and ViewModel/lifecycle wiring is a complete, correct copy of the template — no crashes, no lifecycle bugs.
- Dependencies and Gradle wrapper are pinned and consistent between the root and app `build.gradle` files.
- The repo is small and tidy for what it is: 55 tracked files, one commit, no dead directories.

## What I'd change

- **Authentication is entirely fake.** `LoginDataSource.java:22` reads `// TODO: handle loggedInUser authentication`; the `username` and `password` arguments are never inspected, so any credentials produce `Result.Success`. `logout()` (line 34) is `// TODO: revoke authentication` and is never called anyway.
- **The Firebase write is unconditional and unguarded.** It fires before the try block, has no error callback, and targets a placeholder `message` node — a demo ping to a live database from the auth path.
- **`app/google-services.json` is committed** with the API key `AIzaSyA5BMBAGmYgi66AXlEUohaYCH0ZDXIUwLA`, OAuth client IDs and the database URL.
- **Login leads nowhere**: `finish()` on success with only one declared activity means a valid login closes the app. The "successful logged in experience" TODO at `LoginActivity.java:124` was never started.
- **No README, no license, no CI**, and `.idea/` (including `caches/gradle_models.ser`, 310 lines of binary) plus `FirewallHackathon.iml` and `app/app.iml` are committed.

## Still outstanding

- The three `TODO` comments above are the entire remaining backlog — there is no second screen, no game logic, no data model beyond `LoggedInUser`.
- Tests are the untouched template files: `ExampleUnitTest.java` asserts `2 + 2 == 4`; `ExampleInstrumentedTest.java` checks the package name. Nothing exercises `LoginDataSource` or Firebase.
- `git log` shows a single commit ("First Commit") — the hackathon work was never iterated on in this repository.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
