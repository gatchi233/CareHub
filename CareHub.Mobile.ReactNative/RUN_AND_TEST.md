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
cd "ProjectIdea\CareHub"
docker compose up -d
dotnet run --project .\CareHub.Api\CareHub.Api.csproj
```

If Docker already created the container and you only need to restart it:

```powershell
docker start carehub-postgres
```

Confirm:

- `http://localhost:5007/health`
- `http://localhost:5007/swagger`

## 3) Start Metro

Open a new terminal:

```powershell
cd "ProjectIdea\CareHub\CareHub.Mobile.ReactNative"
cmd /c npm run start
```

## 4) Run Android app

Open Android emulator first, then in another terminal:

```powershell
cd "ProjectIdea\CareHub\CareHub.Mobile.ReactNative"
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

If you hit CMake/Ninja loop errors (`build.ninja still dirty after 100 tries`), use a short drive path and single worker:

```powershell
cd "C:\Users\sambe\Desktop\Term 5\CSTP 2204\ProjectIdea\CareHub\CareHub.Mobile.ReactNative"
cd android
.\gradlew.bat --stop
cd ..
Get-Process java,javaw -ErrorAction SilentlyContinue | Stop-Process -Force
Remove-Item -Recurse -Force .\android\.gradle -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force .\android\.cxx -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force .\android\app\.cxx -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force .\node_modules\react-native-screens\android\.cxx -ErrorAction SilentlyContinue
```

Also set these in `android/gradle.properties`:

```properties
org.gradle.parallel=false
org.gradle.workers.max=1
```

Then run from a short path:

```powershell
subst X: "C:\Users\sambe\Desktop\Term 5\CSTP 2204\ProjectIdea\CareHub"
cd X:\CareHub.Mobile.ReactNative
cmd /c npm run android
```

Most reliable option on Windows:

```powershell
git clone <repo> C:\src\CareHub
cd C:\src\CareHub\CareHub.Mobile.ReactNative
cmd /c npm run android
```

If you hit NDK error (`did not have a source.properties file`), reinstall NDK:

```powershell
$SdkRoot = "C:\Users\sambe\AppData\Local\Android\Sdk"
$SdkMgr = "C:\Program Files (x86)\Android\android-sdk\cmdline-tools\latest\bin\sdkmanager.bat"
Remove-Item -Recurse -Force "$SdkRoot\ndk\26.1.10909125" -ErrorAction SilentlyContinue
& $SdkMgr --sdk_root=$SdkRoot --install "ndk;26.1.10909125"
& $SdkMgr --sdk_root=$SdkRoot --licenses
Test-Path "$SdkRoot\ndk\26.1.10909125\source.properties"
```

## 5) API base URL used by mobile

Configured in `src/services/apiClient.js`:

- Android emulator: `http://10.0.2.2:5007/api`
- iOS/default: `http://localhost:5007/api`

## 6) Quick login and feature test checklist

### Nurse

- Login works
- Can open: Dashboard, Residents, Observations, Medications, MAR, Orders, AI
- Can create, edit, and delete residents
- Can create, edit, and delete medications
- Can adjust medication stock and review low-stock items
- Can create, edit, and delete observations
- Can create and void MAR entries and view 24h MAR report
- Can create medication orders and update statuses
- Can run all mobile AI actions

### General CareStaff

- Login works
- Can open: Dashboard, Residents, Observations
- Can create, edit, and delete observations
- No medication management actions

### Observer

- Login works
- Can open: Dashboard, Observations, Medications
- No create/edit actions

### Admin

- Login works
- Access is blocked on mobile

## 7) Useful test accounts

Allowed on mobile:

- `nurse1` / `nurse123` (Nurse)
- `carestaff1` / `care123` (General CareStaff)
- `resident1` / `resident123` (Observer)
- `resident2` to `resident16` / `resident123` (Observer)

Blocked on mobile:

- `admin` / `admin123` (Admin)

## 8) Optional iOS run (macOS only)

```bash
cd CareHub.Mobile.ReactNative
npm run ios
```
