# ProductivityHub — Android (.NET MAUI Blazor Hybrid)

A native Android port of the ProductivityHub web app. It reuses the original
C# models, EF Core data layer, and business-logic services, and rebuilds the
Razor Pages UI as **Blazor components** hosted in a MAUI `BlazorWebView`.

The app runs fully on-device: a single local user, an on-device SQLite
database, and no server or login.

---

## What changed from the web app

| Web app | This Android app |
|---|---|
| ASP.NET Identity, multi-user, login | **No auth** — one local user (`Local.UserId = "local-user"`), seeded on first run |
| SQL Server / server-hosted SQLite | **On-device SQLite** at `FileSystem.AppDataDirectory/productivityhub.db3` |
| Razor Pages (`.cshtml` + PageModel) | **Blazor components** (`.razor`) |
| `/api/*` minimal-API endpoints + `fetch` | Direct in-process service / `DbContext` calls + small JS interop |
| Scoped `AppDbContext` per request | **`IDbContextFactory<AppDbContext>`** — a fresh short-lived context per operation (correct for the long-lived Blazor host) |

Everything else — the productivity score, water/calorie tracking, habits &
streaks, kanban tasks, Eisenhower matrix, journal & mood, analytics, the
rule-based insight engine, and XP / levels / achievements — is the original
logic, carried over.

---

## Prerequisites

1. **.NET 9 SDK**
2. **MAUI Android workload:**
   ```
   dotnet workload install maui-android
   ```
3. **Android SDK** + an emulator or a physical device with USB debugging.
   (Installing the workload from Visual Studio / VS Code's .NET MAUI extension
   pulls the Android SDK for you.)

## Build & run

From the project folder:

```bash
# list devices/emulators
dotnet build -t:Run -f net9.0-android
```

Or open the folder in Visual Studio / VS Code (with the .NET MAUI extension),
pick an Android target, and press Run.

The database is created and seeded automatically on first launch
(`MauiProgram.CreateMauiApp` → `EnsureCreated()` + `DbSeeder.Seed`).

---

## Project structure

```
ProductivityHubMaui/
├─ ProductivityHubMaui.csproj   # MAUI Blazor, net9.0-android
├─ MauiProgram.cs               # DI, DbContextFactory, DB create+seed
├─ App.xaml(.cs) / MainPage     # hosts the BlazorWebView
├─ Platforms/Android/           # MainActivity, manifest, etc.
├─ Models/                      # entities (reused from the web app)
├─ Data/                        # AppDbContext (plain), DbSeeder, Local.UserId
├─ Services/                    # Streak, Gamification, Insight, Recommendation, Dashboard
├─ Components/
│  ├─ Routes.razor / _Imports.razor
│  ├─ Layout/MainLayout.razor   # sidebar nav + theme toggle
│  └─ Pages/                    # Dashboard, Calendar, Tasks, Notes, Journal,
│                               # Health, Habits, Analytics, Insights, Recommendations
└─ wwwroot/
   ├─ index.html                # Blazor host page
   ├─ css/site.css              # original styles (reused)
   └─ js/app.js                 # interop: theme, charts, calendar, kanban, markdown
```

---

## Important: offline vs. CDN

`wwwroot/index.html` currently loads **Bootstrap, Font Awesome, Google Fonts,
Chart.js, FullCalendar, SortableJS, and marked from CDNs.** That means those
screens need an internet connection for full styling/charts on first load.

For a **fully offline** app, vendor those libraries locally:
1. Download each library's CSS/JS and the font files.
2. Drop them in `wwwroot/lib/...` and the fonts in `wwwroot/fonts/...`.
3. Replace the CDN `<link>`/`<script>` URLs in `index.html` with local paths.

The app's own data and logic are already 100% offline — only third-party
styling/chart assets come from CDNs today.

---

## Swapping the insight engine for an LLM

`Services/InsightService.cs` is rule-based behind `IInsightService`. Replace its
internals with a call to your LLM of choice (keep the interface) and every
screen that shows insights upgrades automatically.

---

## Notes / known follow-ups

- The project couldn't be compiled in the environment it was generated in, so
  the first `dotnet build` on your machine is the real validation step.
- If SQLite fails to initialize on a device, ensure the
  `Microsoft.EntityFrameworkCore.Sqlite` package restored its native
  `SQLitePCLRaw` Android runtime assets (a clean `dotnet restore` fixes this).
- App icon/splash are simple placeholder SVGs in `Resources/` — swap in real art.
- Recurring calendar events are expanded for display in a ~3-month window;
  drag-to-move is enabled for one-off events (recurring instances are read-only
  on the grid and editable via the side panel).
```
