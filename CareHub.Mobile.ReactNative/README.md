# CareHub Mobile React Native

React Native mobile client for CareHub, connected to `CareHub.Api` JWT auth and role-based API routes.

## Implemented Features

- Auth/session
  - Login via `POST /api/auth/login`
  - Session validation via `GET /api/auth/me`
  - Token + user persistence in AsyncStorage
  - `Admin` role blocked from mobile login
- Dashboard
  - Live role-aware metrics for residents, medications, and observations
  - Manual refresh and loading/error states
- Residents
  - List view with search and pull-to-refresh
- Observations
  - List view with filter and pull-to-refresh
  - Create observation flow for `Nurse` and `General CareStaff`
- Medications
  - List view with search, stock visibility, and pull-to-refresh
- MAR (Nurse)
  - View recent MAR entries
  - Create MAR entries
  - Void MAR entries
  - Filter entries and optional include-voided toggle
- Medication Orders (Nurse)
  - List orders
  - Create order
  - Update status transitions (`Requested -> Ordered -> Received` and cancel flow)
- AI (Nurse)
  - Shift summary
  - Detect trends
  - Care query
  - Trend explain (3-day and 7-day options)

## Current Role Matrix

- `Admin`
  - Not allowed on mobile
- `Nurse`
  - `Dashboard`, `Residents`, `Observations`, `Medications`, `MAR`, `Orders`, `AI`
- `General CareStaff`
  - `Dashboard`, `Residents`, `Observations`
- `Observer`
  - `Dashboard`, `Observations`, `Medications`

## API Base URL Configuration

Configured in `src/services/apiClient.js`:

- Android emulator default: `http://10.0.2.2:5007/api`
- iOS/default fallback: `http://localhost:5007/api`
- Optional override: `CAREHUB_API_BASE_URL`

## Backend Startup

Start Docker Desktop first, then from repo root start PostgreSQL:

```powershell
docker compose up -d
```

If the container already exists and is stopped, this also works:

```powershell
docker start carehub-postgres
```

Then start the API:

```powershell
dotnet run --project .\CareHub.Api\CareHub.Api.csproj --launch-profile http
```

Verify:

- `http://localhost:5007/health`
- `http://localhost:5007/swagger`

## Run Mobile

1. `npm install`
2. `npm run start`
3. `npm run android` or `npm run ios`

## Windows Build Note

If Android fails with `react-native-screens` CMake/Ninja errors such as `build.ninja still dirty after 100 tries`, the most likely cause is the long project path on Windows.

Recommended workaround:

1. Move or clone the repo to a short path such as `C:\src\CareHub`
2. Run the mobile app from that shorter path

Alternative workaround:

```powershell
subst X: "C:\Users\sambe\Desktop\Term-5\CSTP-2204\ProjectIdea\CareHub"
cd X:\CareHub.Mobile.ReactNative
npm run android
```

## Known Gaps

1. Mobile is still not full parity with desktop for all workflows.
2. No dedicated automated role regression suite yet.
3. Android environment/toolchain stability may vary by local setup.
