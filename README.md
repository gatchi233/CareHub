# CareHub

A healthcare facility management system for managing resident care, medications, observations, and medication administration records (MAR). Built with .NET MAUI (Windows desktop), React (web), React Native (mobile), and ASP.NET Core API, featuring AI-powered clinical assistance via Groq.

## Prerequisites

Before you begin, make sure you have the following installed:

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/) (for Web and Mobile apps)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for the PostgreSQL database)
- [Git](https://git-scm.com/downloads)
- Windows 10/11 (the desktop app targets Windows only)
- **(Optional)** A free [Groq API key](https://console.groq.com/) for AI features

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/gatchi233/CareHub.git
cd CareHub
```

### 2. Start the database

```bash
docker compose up -d
```

This starts a PostgreSQL 16 container on `localhost:15432` with:
- Database: `carehub`
- Username: `carehub`
- Password: `carehub_pw`

### 3. Configure API settings

Edit `CareHub.Api/appsettings.Development.json` and make sure it has the connection string:

```json
{
  "ConnectionStrings": {
    "CareHubDb": "Host=localhost;Port=15432;Database=carehub;Username=carehub;Password=carehub_pw"
  },
  "Groq": {
    "ApiKey": "YOUR_GROQ_API_KEY_HERE",
    "Model": "llama-3.3-70b-versatile"
  }
}
```

> **AI features** work without a Groq key -- the desktop app falls back to mock responses. To enable AI, get a free key from [Groq Console](https://console.groq.com/).

### 4. Run the API

```bash
dotnet run --project CareHub.Api
```

The API starts on **http://localhost:5007**. Verify it's running:
- Swagger UI: http://localhost:5007/swagger
- Health check: http://localhost:5007/health

On first startup, the API automatically:
- Applies database migrations
- Seeds resident and medication data from `SharedData/` JSON files
- Creates default user accounts (see Test Accounts below)
- Seeds 14 days of realistic MAR entries with observations

### 5. Run the Desktop App

In a **separate terminal**:

```bash
dotnet run --project CareHub.Desktop
```

A Windows app window (1200x800) will open with the login screen.

> The desktop app connects to the API at `http://localhost:5007` by default. It works offline too -- data is cached locally and synced when the API becomes available.

### 6. Run the Web App

In a **separate terminal**:

```bash
cd CareHub.Web
npm install
npm run dev
```

The web app starts on **http://localhost:5173** and connects to the API.

### 7. Run the Mobile App

In a **separate terminal**:

```bash
cd CareHub.Mobile.ReactNative
npm install
npx react-native run-android
```

See `CareHub.Mobile.ReactNative/RUN_AND_TEST.md` for detailed setup instructions.

## Test Accounts

| Username | Password | Role | Access |
|----------|----------|------|--------|
| `admin` | `admin123` | Admin | Full access -- manage residents, staff, inventory, reports |
| `nurse1` | `nurse123` | Nurse | Clinical access -- MAR, observations, medications, AI tools |
| `carestaff1` | `care123` | General CareStaff | Basic care tasks |
| `resident1` | `resident123` | Observer | View own data only |

> There are 16 resident accounts (`resident1` through `resident16`), all with password `resident123`.

## Features

### Clinical
- **MAR (Medication Administration Record)** -- Record medications as given, refused, held, or missed with time-based scheduling
- **Observations** -- Log vital signs and clinical notes (blood pressure, temperature, etc.)
- **Resident Management** -- Create and edit resident profiles with allergies, emergency contacts, and doctor info
- **Medication Management** -- Track inventory, scheduling, stock levels, and expiry dates
- **Medication Orders** -- Request, order, receive, and track medication orders with status workflow
- **Resident Reports** -- View and generate patient reports with observation and medication history

### AI-Powered (requires Groq API key)
- **Shift Summary** -- AI-generated 24-hour care summary for a resident
- **Shift Handoff** -- Facility-wide shift handoff summary for all residents
- **Care Query** -- Ask natural language questions about residents and medications
- **Medication Explain** -- Get medication reference info (usage, side effects, notes)
- **Detect Trends** -- Analyze 14-day trends in vitals and medication compliance
- **Trend Explain** -- Plain-language explanation of detected trends
- **Report Draft** -- Generate structured resident reports

### Facility Management
- **Staff Management** -- Create and manage staff accounts (Admin only)
- **Medication Inventory** -- Stock levels, reorder tracking, expiry alerts, low-stock dashboard
- **Floor Plan** -- Visual room layout
- **Medication Batches** -- Batch tracking per medication

## Architecture

```
CareHub.sln
├── CareHub.Api/                    ASP.NET Core 8.0 Web API
│   ├── Controllers/                REST endpoints (Auth, Residents, Medications, MAR, Orders, AI, Staff)
│   ├── Data/                       EF Core DbContext, JWT auth, roles
│   ├── Entities/                   Domain models
│   ├── Migrations/                 Database migrations
│   └── Services/                   MarSeeder, Groq AI service, rate limiter
│
├── CareHub.Desktop/                .NET MAUI Windows Desktop App
│   ├── Pages/Desktop/              UI pages (Home, MAR, Observations, Reports, AI, etc.)
│   ├── Services/                   Service wrappers (API + Local + Sync)
│   │   ├── Remote/                 API HTTP clients
│   │   ├── Local/                  Offline JSON storage
│   │   └── Sync/                   Sync queue, connectivity helper
│   ├── ViewModels/                 MVVM view models
│   └── Models/                     Data models
│
├── CareHub.Web/                    React + Vite Web Application
│   └── src/
│       ├── pages/                  Dashboard, Residents, Observations, Medications, Inventory, MAR, Orders, Staff, AI Dashboard
│       └── components/             StatCard, ListToolbar, PageTabs, SectionMetaPager
│
├── CareHub.Mobile.ReactNative/     React Native Mobile App (Android/iOS)
│   └── src/
│       ├── screens/                Dashboard, Residents, Observations, Medications, MAR, Orders, AI, Login
│       ├── services/               API client with JWT auth
│       └── context/                Auth context provider
│
├── CareHub.Tests/                  Unit tests (xUnit)
├── SharedData/                     JSON seed data (residents, medications, observations, staff)
├── TeamDocs/                       Team documentation and guides
├── _ProjectSpecifications/         SDD, SRD, SPMP documents
└── docker-compose.yaml             PostgreSQL container
```

### Key Design Decisions

- **Offline-first** -- The desktop app stores data locally as JSON and syncs with the API when online. Each service follows a wrapper pattern: API client (remote) + JSON service (local) + wrapper that coordinates between them.
- **JWT + Role-Based Access Control** -- Stateless auth with Admin, Nurse, General CareStaff, and Observer roles.
- **Time-based MAR seeding** -- Demo data groups medications by (resident, time). All meds in the same slot share the same status (given/refused/missed), reflecting realistic nurse workflows.
- **Idempotent sync** -- MAR entries use a `ClientRequestId` to prevent duplicate entries when syncing offline data.
- **AI rate limiting** -- Built-in rate limiter stays within Groq free-tier limits (27 req/min global, 10 req/min per user).

## API Endpoints

All endpoints (except login and health) require a JWT token in the `Authorization: Bearer <token>` header.

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/auth/login` | Login, returns JWT token |
| `GET` | `/api/auth/me` | Get current user info |
| `GET/POST/PUT/DELETE` | `/api/residents/{id}` | Resident CRUD |
| `GET/POST/PUT/DELETE` | `/api/medications/{id}` | Medication CRUD |
| `POST` | `/api/medications/{id}/adjustStock` | Adjust medication stock |
| `GET` | `/api/medications/lowstock` | Get low-stock medications |
| `GET/POST` | `/api/observations` | Observation CRUD |
| `GET/POST` | `/api/mar` | MAR entry listing and creation |
| `POST` | `/api/mar/{id}/void` | Void a MAR entry |
| `GET` | `/api/mar/report` | MAR report with summary |
| `POST` | `/api/mar/seed-demo` | Seed today's demo MAR data (Admin) |
| `GET/POST` | `/api/medicationorders` | Medication order CRUD |
| `PUT` | `/api/medicationorders/{id}/status` | Update order status |
| `GET/POST/PUT/DELETE` | `/api/staff/{id}` | Staff management |
| `POST` | `/api/ai/shift-summary` | AI shift summary for a resident |
| `POST` | `/api/ai/shift-handoff` | AI facility-wide shift handoff |
| `POST` | `/api/ai/care-query` | AI natural language query |
| `POST` | `/api/ai/medication-explain` | AI medication info |
| `POST` | `/api/ai/detect-trends` | AI trend analysis |
| `POST` | `/api/ai/trend-explain` | AI trend explanation |
| `POST` | `/api/ai/report-draft` | AI report generation |
| `GET` | `/health` | Health check |

Full API documentation is available at `/swagger` when the API is running.

## Running Tests

```bash
dotnet test CareHub.Tests
```

All 29 tests cover AI rate limiting, Groq service, and MAR seeder logic.

## Troubleshooting

**"Host can't be null" when starting the API**
- Make sure `appsettings.Development.json` has the `ConnectionStrings` section with `CareHubDb` (see step 3)

**"Connection refused" when starting the desktop app**
- Make sure the API is running on port 5007: `dotnet run --project CareHub.Api`
- Make sure Docker is running and the database container is up: `docker compose ps`
- The desktop app works offline -- it will sync when the API is available

**Docker container name conflict**
- If you get "container name already in use", remove the old one: `docker stop carehub-postgres && docker rm carehub-postgres`
- Then run `docker compose up -d` again

**Database migration errors**
- Ensure PostgreSQL is running: `docker compose up -d`
- Check the connection string in `appsettings.Development.json`
- If migrations are out of sync, reset: `dotnet ef database drop --project CareHub.Api` then re-run `dotnet ef database update --project CareHub.Api`

**API build error: duplicate Content items (NETSDK1022)**
- In `CareHub.Api.csproj`, change `<Content Include="SeedData\**\*.*">` to `<Content Update="SeedData\**\*.*">`

**AI features return mock/placeholder responses**
- Add a valid Groq API key to `appsettings.Development.json` (see step 3)
- Check the API console output for rate limit warnings
- Free tier: 27 requests/min global, 10 requests/min per user

**Build errors**
- Make sure you're using .NET 8.0 SDK: `dotnet --version`
- Restore packages: `dotnet restore`
- Windows only: the MAUI desktop app requires Windows 10/11

## Tech Stack

- **Backend**: ASP.NET Core 8.0, Entity Framework Core, PostgreSQL 16
- **Desktop**: .NET MAUI (Windows), CommunityToolkit.Maui
- **Web**: React, Vite
- **Mobile**: React Native (Android/iOS)
- **AI**: Groq API (Llama 3.3 70B)
- **Auth**: JWT Bearer tokens, BCrypt password hashing
- **Testing**: xUnit
- **Infrastructure**: Docker Compose, Railway (API), Netlify (Web)
- **CI/CD**: GitHub Actions
