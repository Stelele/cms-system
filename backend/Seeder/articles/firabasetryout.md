# FirabaseTryout

A 2019 Android experiment ("Firebase" misspelled in the repo name) that wires a Firebase Realtime Database into the standard Android login template, plus a couple of link-hub screens.

## What it does

- `MainActivity` (the launcher) writes `"Hello, World!"` to `message` and a nested node to `message/gnoewngeie/mary` in Firebase on every launch, then shows a Log In button.
- `ui/login/LoginActivity` is the stock Android form-validation login: `LoginViewModel`/`LoginFormState` validate username and password, and on success route to `Main2Activity`.
- `Main2Activity` (`activity_main2.xml`) shows four images — biz, quillo, venue, vibes — that open UCT portal links (`vula.uct.ac.za`, `books.quillo.io`, `ictsapps.uct.ac.za/classroom`, `varsityvibe.co.za`) via `ACTION_VIEW`, plus Yes/No buttons.
- `GameLogic` opens an empty screen.

## How it works

`app/src/main/java/com/example/firabasetryout/` holds the code. The login flow is the Google login-template layering: `LoginActivity` → `LoginViewModel` → `LoginViewModelFactory` → `data/LoginRepository` → `data/LoginDataSource`. `LoginDataSource.login()` is where Firebase is actually touched: it generates a random UUID, builds a `LoggedInUser(id, "Jane Doe")`, writes it under `Users/<id>`, attaches a `ValueEventListener` whose `onDataChange` stores the snapshot in a local variable and does nothing with it, and returns success.

`FirebaseDatabase.getInstance()` is used directly in both `MainActivity` and `LoginDataSource` — there is no repository or service abstraction around it. `app/google-services.json` supplies the Realtime Database URL (`firewallhackathon-3587c.firebaseio.com`) and API key.

### Stack

Java 8-era Android (compileSdk 29, AGP 3.5.1, minSdk 15), AndroidX appcompat/constraint/material, `com.google.firebase:firebase-database:19.1.0` with the `google-services` Gradle plugin, JUnit 4 + Espresso as declared test dependencies. Repositories still point at `jcenter()`.

## What works well

- The login module is a faithful, complete copy of the Android form template: state validation, error strings, loading spinner and ViewModel wiring all function.
- Image click handling in `Main2Activity` is wrapped in try/catch for `ActivityNotFoundException` with a user-facing toast.
- Manifest activity declarations and layouts are consistent — nothing crashes on navigation between declared screens.

## What I'd change

- **Auth is fake.** `LoginDataSource.login()` (line 30) carries `// TODO: handle loggedInUser authentication`, ignores the password entirely, and always returns `Result.Success`. `logout()` is `// TODO: revoke authentication`. Any password logs in.
- **`app/google-services.json` is committed** with a live API key (`AIzaSyA5BMBAGmYgi66AXlEUohaYCH0ZDXIUwLA`), OAuth client IDs and a certificate hash for project `firewallhackathon-3587c`. Firebase Android keys are not secrets per se, but the whole config — including the database URL — is checked in with no `.gitignore` exclusion.
- **Unauthenticated open database writes.** `MainActivity.onCreate` writes to the database on every launch with no auth check; combined with the placeholder node name `gnoewngeie`, this is demo scratch data hitting a real project.
- **`Main2Activity.ButtonClickListener` bodies are empty** — both branches are literal `//Do something` / `//Do Something` comments. The Yes/No match buttons do nothing.
- **`GameLogic` is a stub**: an activity whose layout (`activity_game_logic.xml`, 8 lines) is an empty `ConstraintLayout`, registered in the manifest but unreachable from any other class.

## Still outstanding

- `// TODO` at `LoginActivity.java:130` ("initiate successful logged in experience") — post-login beyond firing an intent is unfinished.
- `logout()` never clears the Firebase write or session state.
- The only tests are the untouched template files `ExampleUnitTest.java` (16 lines, asserts `2+2==4`) and `ExampleInstrumentedTest.java` — nothing covers `LoginDataSource` or the database writes.
- No README, no license; `.idea/` and `gradle-wrapper.jar` are committed.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
