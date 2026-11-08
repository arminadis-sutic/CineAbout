# CineAbout

Mobile + desktop application for film lovers: reviews, forum, quizzes, watchlists and personalised recommendations.
Seminar project for *Razvoj softvera II* (IB220011).

## Architecture

| Service | Project | Purpose |
|---|---|---|
| `cineabout-api` | `CineAboutApp` | REST API (JWT auth, EF Core, SignalR), publishes messages to RabbitMQ |
| `cineabout-subscriber` | `CineAboutApp.Subscriber` | Separate worker: consumes RabbitMQ messages and sends e-mail (SMTP) and push (Firebase, see *Push notifications (release day) and e-mail*) |
| `cineabout-db` | SQL Server 2022 | Database `220011`, created by EF migrations and filled by the seeder |
| `cineabout-rabbitmq` | RabbitMQ 3.13 | Message broker |

Flutter clients (separate apps):

- `CineAboutApp.UI/CineAbout_ui` - desktop administration (Windows)
- `CineAboutApp.UI/CineAbout_mobile` - mobile app for users (Android)

## Prerequisites

| For | You need |
|---|---|
| Backend (API, worker, SQL Server, RabbitMQ) | Docker Desktop with Compose v2. Nothing else: the images are built from the sources (the .NET 8 SDK is only needed to build or test outside Docker). Free ports: 5218 (API), 1434 (SQL Server), 5672/15672 (RabbitMQ). |
| Desktop admin (Windows) | Flutter (stable, built with 3.47.9) with Windows desktop support: Visual Studio with the *Desktop development with C++* workload, Windows *Developer Mode* on. |
| Mobile (Android) | Flutter, Android SDK and an Android emulator (AVD) or a phone with USB debugging. |

## Running the backend

1. Copy `.env.example` to `.env`. The example holds placeholders only (no secrets); change at least `DB_PASSWORD`/`SQL_SA_PASSWORD` (they must be equal) and `JWT_SECRET`.
   For the submission the real `.env` is delivered as the encrypted archive `.env-tajne.zip` (AES-256) in the repository root; its password is given only on the DL system, never in the repository.
   Extract the archive in the repository root, next to `docker-compose.yml`: it contains `.env` and replaces any `.env` already there. Git ignores the extracted `.env`.
2. Start everything:

   ```bash
   docker compose up --build
   ```

3. Migrations are applied and the database is seeded automatically on API startup (first start: about a minute, until the log of `cineabout-api` says `Application started`).
   Check: http://localhost:5218/api/Health answers `healthy`. No manual SQL step is needed; see *Database and seed data* below.
4. API: http://localhost:5218 (Swagger is available only when `ASPNETCORE_ENVIRONMENT=Development`). RabbitMQ UI: http://localhost:15672.

All configuration lives in `.env`; nothing is hard-coded in source or `appsettings.json` (which only holds empty placeholders and log levels).
Besides the connection settings, `.env` has the token lifetimes `JWT_EXPIRATION_HOURS`, `JWT_REFRESH_EXPIRATION_HOURS` and `JWT_RESET_EXPIRATION_MINUTES`;
the API refuses to start, naming the missing value, if one of them is absent. An older `.env` must get these three lines (see `.env.example`).

### Password reset

`Forgot password?` e-mails a link (`BACKEND_URL/reset-password?token=...`) through RabbitMQ and the subscriber (SMTP settings in `.env`).
Opening it on the phone starts the app on the "new password" screen (`cineabout://app/reset`); the token can also be pasted by hand.

- The token is 32 random bytes, valid for `JWT_RESET_EXPIRATION_MINUTES` minutes (15 in `.env.example`) and works once. Only a keyed hash tied to the user and the purpose is stored.
- A new request invalidates older links; at most one link per account and minute; 8 requests per 15 minutes per client address (`RateLimits__PasswordReset` to change it).
- The answer is the same for known and unknown e-mail addresses.
- A reset, a password change, a role change or a deactivation ends all sessions of that user: refresh tokens are deleted and already issued access tokens stop working (security stamp checked on every request).
- `BACKEND_URL` must be reachable from the device that opens the e-mail.

### Push notifications (release day) and e-mail - setting up the credentials

The flow stays **API -> RabbitMQ -> worker**: the API (reminder dispatcher, every 10 minutes) creates the "releases today" notification, writes it for the user
(in-app, SignalR) and publishes a push message with the device tokens of users who have push switched on; `cineabout-subscriber` sends it through Firebase.
The phone registers its token with `User/register-device` after sign-in and removes it when the user signs out.

**Firebase (push).** Nothing secret is in the repository or in an image.

1. Firebase console -> your project -> Project settings -> Service accounts -> *Generate new private key*. Keep that JSON file safe; it is not committed.
2. Put the **whole JSON on one line, in single quotes**, into `.env`: `FIREBASE_CREDENTIALS_JSON='{"type":"service_account",...}'` and set `FIREBASE_PROJECT_ID=<your project id>`.
   The backslash-n sequences inside the private key stay exactly as they are in the file; single quotes keep Docker Compose from changing anything. No extra file is needed on the machine that runs the project:
   the `.env` (delivered in the encrypted configuration archive) carries the credentials, and the worker reads them at start. They are never part of an image, the APK or the EXE.
   *Optional alternative:* leave `FIREBASE_CREDENTIALS_JSON` empty and set `FIREBASE_CREDENTIALS_PATH=C:/path/to/the-key.json`; `docker-compose.yml` then mounts that file read-only into the worker.
   **Priority:** if both are filled, `FIREBASE_CREDENTIALS_JSON` wins and the file is ignored. If neither has a private key, push is disabled (the worker log says so).
3. `docker compose up -d --build cineabout-subscriber`; with `DELIVERY_MODE=live` the worker log must say `Firebase initialized from FIREBASE_CREDENTIALS_JSON` (or `... from the mounted credentials file`). A malformed JSON value is reported without printing it.
   In `DELIVERY_MODE=test` no Firebase app is created whatever credentials are present.
4. Mobile app: `CineAboutApp.UI/CineAbout_mobile/android/app/google-services.json` (Android client configuration of the package `com.cineabout.cineabout_mobile`, no private key) is part of the repository,
   so the APK build picks it up without any extra step. Push on the phone works only when the stack runs with `DELIVERY_MODE=live` and the service-account key from step 2 is in `.env`.
   Rebuild the APK (`flutter build apk --release --dart-define=API_BASE_URL=http://<PC LAN address>:5218`).
5. Cloud Messaging API (V1) must be enabled for the project. Android 13+ asks the user for the notification permission on first sign-in.

**Test mode.** `DELIVERY_MODE=test` in `.env` makes the worker consume e-mail and push messages **without sending anything** (SMTP is never called and the Firebase app is not
even created; the worker log starts with `DELIVERY MODE = TEST`). Use it for every automated test run; `DELIVERY_MODE=live` (the default) sends normally.

**E-mail (SMTP).** `.env`: `MAIL_HOST`, `MAIL_PORT` (587), `MAIL_ENABLE_SSL=true`, `MAIL_USERNAME` (display name), `MAIL_EMAIL` (sender address) and `MAIL_PASSWORD`
(for Gmail: an *app password* of an account with 2-step verification, not the account password). If `MAIL_EMAIL`/`MAIL_PASSWORD` are empty the worker skips e-mails and says so in its log.

### New accounts

Whether a new registration is active immediately is the administrator setting `AutoActivateNewUsers` (desktop app, Settings; a fresh database is seeded with it on). When it is off, the account is created inactive: the mobile app then shows "Account created. An administrator has to activate it before you can sign in." and returns to the sign-in screen, and the administrator activates the user in the desktop Users screen.

### Database and seed data

The database is named `220011` (the index number). EF Core migrations create the schema (24 domain tables, foreign keys defined in the EF configuration) and the seeder fills it
once: the seeder steps are recorded in `SeedHistories`, so a restart never duplicates rows and never overwrites what an administrator edited.
A start from an empty database gives (measured on a scratch database with the same image):

| Area | Rows |
|---|---|
| Users (10, roles `Admin` / `User`), profiles, streaks (equal to the daily-quiz days of the demo sessions) | 10 / 10 / 10 |
| Titles (all with a poster), people (all with a photo), studios (7 with a logo) | 29 / 121 / 13 |
| Genres, languages, countries, cities, award types, awards | 13 / 9 / 14 / 9 / 6 / 8 |
| Reviews, review votes | 45 / 57 |
| Forum threads, comments, comment votes, thread votes | 12 / 32 / 56 / 41 |
| Watchlist / Interested rows, swipes (recommender input), follows | 32 / 60 / 13 |
| Quiz questions, categories, sessions | 33 / 5 / 22 |
| Reports (moderation queue), reminders, notifications | 3 / 8 / 5 |

Four demo accounts also own a few lists with titles (`UserList`), and there are no demo reminders (a reminder needs a real upcoming title with its TMDB id). Later corrections of demo data are small
versioned steps (`SeedHistories` keys such as `DemoLists-v1`): each runs once per database and touches only the rows it names.
The demo catalog (titles, genres, directors, cast with characters, synopses) is curated in `CatalogSeeder`. Posters, actor photos and studio logos are **links** to
`image.tmdb.org` (`CatalogImages`), so no TMDB token is needed, but the machine needs internet access to show the pictures. The seeded users have no profile photo (the app shows their initials).

### Technical tables and their relations

Every table is connected to the data it describes by a foreign key, except three that have nothing to point at:

| Table | What it is for | Why it has no foreign key |
|---|---|---|
| `AppSettings` | One row with the administrator's settings (today: whether a new registration is active at once, `AutoActivateNewUsers`; desktop *Settings* screen). | It is a single settings record; no other row belongs to it. |
| `SeedHistories` | One row per data-seed step that already ran (`Seed.InitialCatalog.v1`, `DemoLists-v1`, ...). On every start the API looks here to run each step only once, so a restart never duplicates data or overwrites what an administrator edited. | A marker is a name and a date; it does not describe any entity. |
| `PendingFileDeletions` | A queue of file paths (old profile pictures) that must be deleted from disk. The row is written in the same database save as the change that made the file obsolete; a background service deletes the file and removes the row, and retries if the file is locked. | A file path is not an entity of the database; the row is a work item. |

`AuditLogs.PerformedByUserId` and `BlacklistedTokens.UserId` (who signed out, set at sign-out; older rows have no owner recorded) are real foreign keys to the users.
Accounts are anonymised, never removed, so these links stay valid when an account is deleted.

### Design note: notifications

`IUserNotifier` (used after a review is removed, a comment, a follow, a report decision, a release day) calls `NotificationService.CreateAsync`, which saves the notification and then sends the in-app / push messages
through RabbitMQ and SignalR. This runs **after** the main operation has been committed and its failure is caught, so a notification can never undo what a user just did. The other side of that: **the main operation can succeed and the notification be missing** if the follow-up step fails (database error while saving it, RabbitMQ or SignalR unavailable). The failure is only logged; nothing is retried or recorded for later. This is a plain post-commit call, **not** a transactional outbox (an outbox would store the message in the same transaction as the change and deliver it durably afterwards). It is a documented deviation chosen by the author; whether the course accepts it is not known. Moving the notification into the main
operation's single save would need the push / SignalR messages to wait for the commit (a post-commit hook, i.e. an outbox) and would make a failing notification roll the user's action back; that is why it is a documented exception.

### TMDB (upcoming releases and reminders)

Set `TMDB_ACCESS_TOKEN` in `.env` to a TMDB *API Read Access Token* (https://www.themoviedb.org/settings/api).
Without it, `GET /api/Reminder/upcoming` answers with a clear "not configured" message and everything else keeps working.
A background service in the API creates the "releases today" notification (in-app and SignalR; the push message is published too, see *Push notifications (release day) and e-mail*).

## API overview

All routes are under `/api` and require a JWT, except: `User/login`, `register`, `refresh`, `reset-password`, `reset-password/confirm`, the country list `Country` (needed by the registration form), `Health`, and the reset link page `/reset-password`. The current user always comes from the token.

| Area | Endpoints |
|---|---|
| Auth / users | `User/login`, `register`, `refresh`, `logout`, `reset-password` (e-mails a link), `reset-password/confirm`, `User/me`, `UserProfile/me` (photo upload: JPG/PNG/WEBP, max 2 MB, declared type and real content are checked, the old file is removed; the city is chosen by `CityId` from `City`), `UserProfile/{id}/image/{file}` (profile picture download: signed-in users only; the owner and administrators always, others while the profile is public; the pictures are not public static files), `UserProfile/me/genres` |
| Movies | `Movie` (read when signed in; admin write), `Genre`, `Language`, `Country`, `City`, `Person` and `Studio` (+ `{id}/movies` for the person / studio pages), `AwardType` |
| Reviews | `Review` (read when signed in, user create/edit/delete own), `Review/{id}/vote`, `Review/summary/{movieId}` (average + histogram) |
| Forum | `Thread` (tags, spoiler flag, sort), `Thread/tags`, `Thread/{id}/vote`, `Comment`, `Comment/{id}/vote` |
| Moderation | `Report` (users report content; admin decides via `Report/{id}/decision`), `Review/{id}/status`, `Thread/{id}/status` |
| Lists / social | `Watchlist` (Watchlist + Interested), `UserList` (lists a user creates: create/rename/delete, `UserList/{id}/items/{movieId}`; readable by every signed-in user, `?userId=` for another profile), `Follow/{userId}`, `Follow/feed` (also shows "created a list") |
| Quiz | `Quiz/daily/start`, `Quiz/free/start`, `Quiz/submit`, `Quiz/streak`, `Quiz/leaderboard`, `Quiz/categories` |
| Discover | `Discover` (explainable recommendations), `Discover/swipe` |
| Reminders | `Reminder/upcoming`, `Reminder` (paged like every list, `PageSize` at most 100) |
| Admin | `Dashboard/*`, `Export/community-activity`, `Export/content-popularity` (PDF), `AuditLog`, `QuizQuestion` (category by `categoryId`), `QuizCategory`, `Role` (create / rename / delete; `Admin` and `User` are fixed), `AppSettings` |

Create/update requests are validated on the server (required fields, lengths, year/date ranges, existing references, enum values) and answer `400` with `{"errors": {...}}` and a readable message. A review that an administrator removed is hidden from everyone else, also when read by id (`404`).

Enums are accepted and returned as strings (e.g. `"Interested"`); numeric values are accepted as well.

## Running the Flutter apps

The API address is never hard-coded: both apps read it with `String.fromEnvironment('API_BASE_URL')`.

| Client | Default (used by the submission builds) | Other targets |
|---|---|---|
| Desktop (Windows) | `http://localhost:5218` | any host: `--dart-define=API_BASE_URL=http://<host>:5218` |
| Android emulator (AVD) | `http://10.0.2.2:5218` (the emulator's name for the host PC) | - |
| Physical phone | not usable with the default | `--dart-define=API_BASE_URL=http://<PC-LAN-address>:5218` (see below) |

Desktop admin (Windows; Windows *Developer Mode* must be on for Flutter plugins):

```bash
cd CineAboutApp.UI/CineAbout_ui
flutter run -d windows --dart-define=API_BASE_URL=http://localhost:5218
```

Mobile app (Android; the emulator reaches the host machine at `10.0.2.2`, which is the default):

```bash
cd CineAboutApp.UI/CineAbout_mobile
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5218
# physical phone on the same network: use the PC's LAN address (ipconfig), not 10.0.2.2
```

The default `10.0.2.2` exists only inside the Android emulator. A release APK built without `--dart-define=API_BASE_URL=...` shows
"Cannot reach the server" on a real phone. For a phone: phone and PC on the same Wi-Fi/LAN, build with the PC's LAN address
(`ipconfig`), and allow inbound TCP 5218 for the local network in Windows Firewall (the API listens on all interfaces: `API_HOST_BIND=0.0.0.0`).
A quick check from the phone's browser: `http://<PC LAN address>:5218/api/Health` must show `{"status":"healthy",...}`.

Tests: `flutter test` in `CineAbout_ui` runs the widget tests. Each app also has integration tests that sign in with a seeded account against a running API:
desktop `flutter test integration_test -d windows --dart-define=API_BASE_URL=http://localhost:5218`; mobile (needs an Android emulator or phone, there is no Windows target)
`flutter test integration_test -d <device-id> --dart-define=API_BASE_URL=http://10.0.2.2:5218` (use the LAN address on a phone).
Known limitation: `flutter test` **uninstalls the app from the device when it finishes**: use an emulator you can reset, not a phone with data you care about. The mobile test closes the
soft keyboard and scrolls each control into view before tapping it (the keyboard covered the buttons on a real phone); that corrected version has not been run on a device yet.

## Release builds for the submission

Build files are not committed; they are attached to the GitHub Release as one ZIP (`fit-build-YYYY-MM-DD.zip`). Build with the **default** addresses (no `--dart-define`):

```bash
cd CineAboutApp.UI/CineAbout_mobile
flutter clean
flutter build apk --release          # build/app/outputs/flutter-apk/app-release.apk   (API 10.0.2.2:5218, Android emulator)

cd ../CineAbout_ui
flutter clean
flutter build windows --release      # build/windows/x64/runner/Release/                (API localhost:5218)
```

The ZIP holds the APK and the `Release` folder of the desktop build, never `.env`, a service-account key or `google-services.json`.
A build made with a LAN address (for a physical phone) is a local test build and must not be the one submitted.


## Test accounts

| Context | Username | Password |
|---|---|---|
| Desktop (Admin) | `desktop` | `test` |
| Mobile (User) | `mobile` | `test` |

Roles: `Admin`, `User`.

## Recommender

See [recommender-dokumentacija.md](recommender-dokumentacija.md).

## Database migrations

```bash
dotnet tool restore
dotnet dotnet-ef migrations add <Name> --project CineAboutApp.Services --startup-project CineAboutApp --output-dir Migrations
```
