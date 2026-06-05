# Technical Stack

## Current Stack

- Frontend: React, React Router, Vite, TypeScript, Tailwind CSS, lucide-react
- Backend: ASP.NET Core minimal APIs targeting .NET 10
- Backend persistence: EF Core with Npgsql
- Database: PostgreSQL 16 through Docker Compose
- Cache/queue foundation: Redis 7 through Docker Compose
- Package manager: npm
- Frontend test command: `npm test`
- Local frontend port: `5174`
- Local backend URL: `http://localhost:5080`

Current runtime/configuration details:

- The frontend app requires Node.js `>=20.19.0` according to `src/app/package.json`.
- Docker Compose uses `COMPOSE_PROJECT_NAME` for isolated local clone resources and includes an optional `api` profile for building the backend container from the current clone's code.
- Server deployment templates live under `deploy/` and are separate from the root local-development Compose file.
- The reusable deployment kit builds an API image from `src/api/Dockerfile` and a web image from `src/app/Dockerfile`, then serves the frontend through Nginx behind an optional Caddy proxy.
- Vite loads the repository root `.env` through `envDir`, binds to `VITE_APP_HOST`/`VITE_APP_PORT`, uses strict ports, and proxies `/api` and `/health` to `VITE_API_BASE_URL`.
- The backend loads the nearest `.env` file, derives PostgreSQL/Redis connection strings, maps `AUTH_COOKIE_NAME`, and configures local CORS from `VITE_APP_HOST`/`VITE_APP_PORT`.
- In non-development environments, the backend uses forwarded headers and secure cookies so it can run behind the deployment proxy. Temporary HTTP-only staging can opt out with `AUTH_COOKIE_REQUIRE_SECURE=false`; production should keep it `true`.
- The API uses minimal endpoint modules discovered through `IPlatformApiModule`; it does not use controllers yet.
- EF Core persistence lives in `src/api/Infrastructure/Persistence`, domain entity bases in `src/api/Domain/Common`, domain entities in `src/api/Domain/Entities`, and CRUD application primitives in `src/api/Application/Common`.
- Internal persisted entities use PostgreSQL `uuid` / C# `Guid` IDs, with external auth IDs stored separately on users.
- Cookie authentication uses a server-only bootstrap admin fallback and local PostgreSQL users. Effective permissions come from role permissions and per-form role access rows.
- The current Forms module exposes persisted list/create, backend draft update, publish, published rendering, and form access option endpoints.
- The current Records module exposes submit, list, detail, edit, and soft-delete endpoints.
- The current Reports module exposes V2 saved list report definition endpoints.

## Current Frontend Foundation

- TypeScript is enabled in strict mode.
- Tailwind CSS theme tokens and shared UI components already exist.
- The real app and `/theme` playground share the same shell and UI primitives.
- Current real app feature code includes `features/forms` for schema, form APIs, list/create, backend-owned builder behavior, renderer/preview/submission helpers, `features/records` for records pages/helpers, `features/users` for Users & Access APIs/types/pages, and `features/reports` for V2 list report definition APIs/types/pages.

## Recommended Frontend Additions

Use these only when a task needs them:

- React Hook Form
- Zod or another validation library
- TanStack Query for server state
- Zustand only for builder/editor state
- Vitest or Jest for unit tests
- React Testing Library for component tests
- Playwright for E2E tests later

## Recommended Backend Additions

Use these only when a task needs them:

- ASP.NET Core Web API
- FluentValidation or built-in validation
- ASP.NET Core Identity, JWT, or an external provider if the local cookie-auth foundation needs to integrate with enterprise identity later
- xUnit or NUnit for tests
- Testcontainers later for PostgreSQL integration tests

## PostgreSQL Recommendations

Use PostgreSQL for:

- Users/roles/groups/departments
- Forms and form versions
- Records and record values
- Reports
- Permissions
- Triggers
- Audit logs

Use JSONB for flexible platform configuration:

- Form fields schema
- Form layout schema
- Record values
- Report configuration
- Trigger configuration
- Print template configuration

Use normal relational columns for common filters:

- `form_id`
- `form_version_id`
- `status`
- `owner_id`
- `department_id`
- `created_by_id`
- `created_at`
- `updated_at`

## Recommended Build Commands

Frontend:

```bash
cd src/app
npm install
npm test
npm run build
```

Backend:

```bash
dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj
dotnet ef migrations has-pending-model-changes --project src/api/OpenBusinessPlatform.Api.csproj --startup-project src/api/OpenBusinessPlatform.Api.csproj
cd src/api
dotnet restore
dotnet build
```

The API project uses `src/api/Directory.Build.props` to place generated build output under `.artifacts/api`.

Run local services:

```bash
cp .env.example .env
docker compose up -d
```

Render the reusable deployment Compose configuration:

```bash
docker compose --profile migrate --env-file deploy/env/stage.env.example -f deploy/compose.yml -f deploy/compose.stage.example.yml -f deploy/compose.proxy.yml config
```

Run development servers:

```bash
cd src/api
dotnet run
```

```bash
cd src/app
npm run dev
```

The backend test project is a lightweight executable test harness until a formal xUnit/NUnit project is introduced.

## Local Environment

Local development can use a root `.env` file copied from `.env.example`. Defaults work without `.env`, but bootstrap admin login and multi-clone isolation should be configured per clone.

- Docker Compose reads `COMPOSE_PROJECT_NAME`, `POSTGRES_*`, `REDIS_PORT`, `API_PORT`, and optional API profile values.
- The ASP.NET Core API loads the nearest `.env` file for local development, derives connection strings from `POSTGRES_*` and `REDIS_*`, maps bootstrap admin variables into `BootstrapAdmin` options, and maps `AUTH_COOKIE_NAME` into the auth cookie option.
- Vite reads the root env file through `envDir` and uses `VITE_APP_HOST`, `VITE_APP_PORT`, `VITE_API_BASE_URL`, and the non-secret `BRAND_LOGO_TEXT` value.
- Admin credentials must remain server-only. Do not add admin password values to `VITE_` variables.

## Important Library Decision

Do not add XYFlow in V1.

`@xyflow/react` is used only for the V5 workflow visual builder. Do not use it for form layout, reports, permissions, or basic trigger authoring.
