# CareHub Run Guide

This guide covers the current local run steps for:

- API
- Desktop
- Web
- Mobile (React Native)

Use a short local path on Windows when possible. Recommended clone path:

```powershell
C:\src\CareHub
```

## 1. Prerequisites

- Docker Desktop
- .NET 8 SDK
- Node.js 18+
- Windows 10/11
- Android Studio + emulator for mobile Android
- JDK 17 for React Native Android builds

## 2. Start Database

From repo root:

```powershell
docker compose up -d
```

Expected:

- PostgreSQL runs on `localhost:5433`
- Container name: `carehub-postgres`

If the existing container is broken:

```powershell
docker rm -f carehub-postgres
docker compose up -d
```

Verify:

```powershell
docker ps
```

## 3. Start API

From repo root:

```powershell
dotnet run --project .\CareHub.Api\CareHub.Api.csproj --launch-profile http
```

Expected:

- API base URL: `http://localhost:5007`
- Health: `http://localhost:5007/health`
- Swagger: `http://localhost:5007/swagger`

Important:

- Start the API before Web, Desktop, or Mobile if you want live server-backed behavior.
- The API applies migrations and seed logic on startup.

## 4. Run Desktop Version

From repo root, in a new terminal:

```powershell
dotnet run --project .\CareHub.Desktop\CareHub.Desktop.csproj
```

Notes:

- Desktop targets Windows only.
- Default API target is `http://localhost:5007`.
- Desktop supports offline/local fallback behavior, but start the API for the normal full workflow.

## 5. Run Web Version

From repo root, in a new terminal:

```powershell
cd .\CareHub.Web
npm.cmd install
npm.cmd run dev
```

Expected:

- Vite dev server usually starts on `http://localhost:5173`
- Web app calls the API at `http://localhost:5007/api`

Common issue:

- `npm run start` will fail because this project uses Vite, not a `start` script.
- Use `npm.cmd run dev`

## 6. Run Mobile Version

Use the short clone path if possible:

```powershell
C:\src\CareHub
```

### 6.1 Start Metro

In one terminal:

```powershell
cd C:\src\CareHub\CareHub.Mobile.ReactNative
npm.cmd install
npm.cmd run start
```

Leave this terminal open.

If Metro says port `8081` is already in use, either reuse the existing Metro session or kill the old process:

```powershell
netstat -ano | findstr :8081
taskkill /PID <PID> /F
```

Then restart Metro:

```powershell
npm.cmd run start -- --reset-cache
```

### 6.2 Run Android

Start the emulator first, then in a second terminal:

```powershell
cd C:\src\CareHub\CareHub.Mobile.ReactNative
npm.cmd run android
```

Expected:

- App installs on the emulator
- App connects to Metro on port `8081`
- App talks to API through `http://10.0.2.2:5007/api`

### 6.3 If the app says "Unable to load script"

That means Metro is not available.

Fix:

1. Keep `npm.cmd run start` running in a separate terminal.
2. Reload the app.
3. If needed, rerun:

```powershell
npm.cmd run android
```

### 6.4 Windows native build issue

If Android build fails with `build.ninja still dirty after 100 tries`, use the short path clone and clear native caches:

```powershell
cd C:\src\CareHub\CareHub.Mobile.ReactNative\android
.\gradlew.bat --stop
cd ..
Remove-Item -Recurse -Force .\android\.gradle -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force .\android\.cxx -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force .\android\app\.cxx -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force .\node_modules\react-native-screens\android\.cxx -ErrorAction SilentlyContinue
npm.cmd run android
```

## 7. Seeded Login Accounts

API/Web/Desktop:

- `admin` / `admin123`
- `staff1` / `staff123`
- `observer1` / `observer123`
- `resident1` / `resident123`

Mobile:

- `nurse1` / `nurse123`
- `carestaff1` / `care123`
- `resident1` / `resident123`

Note:

- Admin access is intentionally blocked on mobile.

## 8. Recommended Run Order

1. `docker compose up -d`
2. `dotnet run --project .\CareHub.Api\CareHub.Api.csproj --launch-profile http`
3. Start the client you need:
   - Desktop: `dotnet run --project .\CareHub.Desktop\CareHub.Desktop.csproj`
   - Web: `cd .\CareHub.Web && npm.cmd run dev`
   - Mobile Metro: `cd C:\src\CareHub\CareHub.Mobile.ReactNative && npm.cmd run start`
   - Mobile Android: `cd C:\src\CareHub\CareHub.Mobile.ReactNative && npm.cmd run android`

## 9. Current Version Notes

- API current local port is `5007`, not `5001`.
- Web uses Vite.
- Mobile React Native should be run from a short Windows path to avoid native build failures.
- Docker container name is `carehub-postgres`.
