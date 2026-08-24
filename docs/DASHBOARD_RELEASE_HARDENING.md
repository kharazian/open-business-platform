# Dashboard Release Hardening

Status: implemented and automated.

## Scope

The dashboard release gate covers the highest-risk lifecycle boundary: editable draft state must never become viewer-visible before an explicit publish. It also verifies preview/save behavior, repeated publishing, revision history, restore semantics, unpublishing, manager-only history access, and cleanup.

## Run locally

Start PostgreSQL and Redis, then run the dashboard suite from the frontend directory:

```bash
docker compose up -d
cd src/app
npm install
npx playwright install chromium
npm run test:e2e:dashboard
```

Playwright starts the API and Vite when their configured ports are free. In local development it reuses healthy servers already listening on `5080` and `5174`. CI does not reuse existing processes.

## Isolation and cleanup

The test uses a timestamped `E2E dashboard ...` name and slug. It never edits the seeded Business Performance Sample dashboard. A `finally` cleanup calls the permission-protected dashboard soft-delete endpoint with the latest concurrency stamp. Before each run it also removes dashboards left by an interrupted earlier E2E process.

## Release commands

```bash
cd src/app
npm test
npm run build
npm run quality:dashboard
npm run test:e2e:dashboard

cd ../..
dotnet run --no-restore --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj
dotnet build --no-restore src/api/OpenBusinessPlatform.Api.csproj
dotnet ef migrations has-pending-model-changes --project src/api/OpenBusinessPlatform.Api.csproj --startup-project src/api/OpenBusinessPlatform.Api.csproj
```

The NuGet `NU1900` vulnerability-feed warning can appear when `api.nuget.org` is unavailable. It is not a compile or test failure, but the feed should be reachable in CI so vulnerability auditing remains active.
