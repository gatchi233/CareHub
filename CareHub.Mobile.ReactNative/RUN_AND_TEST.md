# CareHub Mobile (React Native) - Run and Test Guide

## 1) Prerequisites

- Node.js 18+
- Android Studio (with at least one Android emulator)
- JDK 17
- .NET 8 SDK
- Docker Desktop

## 2) Start the backend API first

From repo root:

```powershell
cd "C:\Users\sambe\Desktop\Term 5\CSTP 2204\ProjectIdea\CareHub"
docker start carehub-postgres
dotnet run --project .\CareHub.Api\CareHub.Api.csproj
```

Confirm:

- `http://localhost:5001/health`
- `http://localhost:5001/swagger`

## 3) Start Metro

Open a new terminal:

```powershell
cd "C:\Users\sambe\Desktop\Term 5\CSTP 2204\ProjectIdea\CareHub\CareHub.Mobile.ReactNative"
cmd /c npm run start
```

## 4) Run Android app

Open Android emulator first, then in another terminal:

```powershell
cd "C:\Users\sambe\Desktop\Term 5\CSTP 2204\ProjectIdea\CareHub\CareHub.Mobile.ReactNative"
cmd /c npm run android
```

If Gradle cache errors happen, clean and retry:

```powershell
cd "C:\Users\sambe\Desktop\Term 5\CSTP 2204\ProjectIdea\CareHub\CareHub.Mobile.ReactNative"
cd android
.\gradlew.bat --stop
cd ..
Remove-Item -Recurse -Force .\android\.gradle -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force .\android\build -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force .\android\app\build -ErrorAction SilentlyContinue
cmd /c npm run android
```

## 5) API base URL used by mobile

Configured in `src/services/apiClient.js`:

- Android emulator: `http://10.0.2.2:5001/api`
- iOS/default: `http://localhost:5001/api`

## 6) Quick login and feature test checklist

### Nurse

- Login works
- Can open: Dashboard, Residents, Observations, Medications
- Can create observation

### General CareStaff

- Login works
- Can open: Dashboard, Residents, Observations
- Can create observation
- No medication management actions

### Observer

- Login works
- Can open: Dashboard, Observations, Medications
- No create/edit actions

### Admin

- Login works
- Access is blocked on mobile

## 7) Useful test accounts

- `admin` / `admin123`
- `nurse1` / `nurse123`
- `carestaff1` / `care123`
- `resident1` / `resident123`

## 8) Optional iOS run (macOS only)

```bash
cd CareHub.Mobile.ReactNative
npm run ios
```
