# Open Business Platform

Open Business Platform is an open-source modular low-code platform for building internal business applications.

The first product direction is a business form platform: users can create responsive forms, publish versioned forms, collect records, view records and reports, control access, print data, and later automate work with triggers and approvals.

## Current Stack

- Backend: ASP.NET Core minimal APIs targeting .NET 10
- Frontend: React, React Router, Vite, TypeScript, Tailwind CSS, lucide-react
- Database: PostgreSQL
- Cache/queue foundation: Redis
- Local orchestration: Docker Compose

## Current State

The repository currently includes:

- ASP.NET Core API in `src/api`
- React frontend in `src/app`
- Frontend module registry in `src/app/src/modules` and `src/app/src/platform`
- Shared frontend UI/layout components
- Shared V1 form schema types and validators in frontend/backend code
- EF Core/Npgsql persistence foundation for users, roles, departments, forms, form versions, records, role permissions, form permissions, and audit logs
- Cookie authentication with a bootstrap admin fallback plus local PostgreSQL users
- Users & Access workspace for local users, roles, menu permissions, and per-form role access
- Forms list/create API and frontend page backed by PostgreSQL
- Backend-owned form builder route at `/forms/:formId/builder` for draft metadata, fields, responsive layout, preview, and publishing
- Published form submission route at `/forms/:formId/submit`
- Record list/detail/edit/delete flows with backend value validation, permission checks, soft delete, and audit logging
- Browser print support for record lists and record details
- Development seed data for demo users, roles, departments, a published Employee Information Form, per-form access, and 10 sample records
- Main app appearance settings with palette, color mode, density, layout, radius, and shadow controls saved in browser `localStorage`
- Permission-aware main app routes for dashboard, forms, users/access, reports, settings, and profile
- Saved V2 preview list report definitions with selected columns, one UI filter, one UI sort, backend validation, and permission-checked persistence
- `/theme` playground for sample-data UI, layout, component, workspace, and authentication demos
- Docker Compose services for PostgreSQL and Redis
- API health endpoint at `http://localhost:5080/health`
- Development API explorer at `http://localhost:5080/swagger` and `http://localhost:5080/scalar`
- Authenticated API dashboard summary endpoint at `http://localhost:5080/api/dashboard/summary`

## Product Direction

V1 focuses on:

- Form list and form draft creation: complete
- Basic field builder: complete
- Responsive layout builder: complete
- Form preview and publish: complete
- Record submission and record management: complete
- Basic permissions: complete
- Browser printing: complete
- Audit logs: complete
- Seed/demo data: complete

V1 is the finalized foundation. Current V2 work starts with saved list report definitions, then continues with report viewer/run behavior, CSV export, and cleaner print layouts. Later versions add advanced permissions, triggers, workflows, PDF templates, dashboards, integrations, and enterprise features.

## Prerequisites

- Docker and Docker Compose
- .NET 10 SDK
- Node.js 20.19 or newer
- npm

## Run Locally

Create your local environment file:

```bash
cp .env.example .env
```

The `.env` file is optional for defaults, but recommended when you need bootstrap admin credentials or want to run multiple clones side by side.

Start PostgreSQL and Redis:

```bash
docker compose up -d
```

Run the backend API:

```bash
cd src/api
dotnet run
```

The API listens on:

```text
http://localhost:5080
```

Check the health endpoint:

```bash
curl http://localhost:5080/health
```

In development, browse the backend API with either UI:

```text
http://localhost:5080/swagger
http://localhost:5080/scalar
```

The generated OpenAPI document is available at:

```text
http://localhost:5080/openapi/v1.json
```

The app signs in with the server-only bootstrap admin from `.env`:

```text
BOOTSTRAP_ADMIN_EMAIL=admin@company.test
BOOTSTRAP_ADMIN_PASSWORD=change-me-before-use
```

The bootstrap admin is a setup fallback with full built-in permissions. Local users and roles can also sign in after they are created through Users & Access.

In development, API startup also seeds demo database users when PostgreSQL is available and migrations have been applied:

```text
admin.demo@company.test
builder.demo@company.test
user.demo@company.test
viewer.demo@company.test
```

All seeded demo users use:

```text
DemoUser!2026
```

Run the frontend in another terminal:

```bash
cd src/app
npm install
npm run dev
```

Open:

```text
http://localhost:5174
```

Useful frontend routes:

- `/`
- `/dashboard`
- `/login`
- `/forms`
- `/forms/:formId/builder`
- `/users`
- `/reports` V2 preview
- `/settings`
- `/profile`
- `/theme`
- `/theme/users`
- `/theme/forms`
- `/theme/components`
- `/theme/layouts`
- `/theme/login`

## Build

Frontend:

```bash
cd src/app
npm test
npm run build
```

Backend:

```bash
dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj
dotnet ef migrations has-pending-model-changes --project src/api/OpenBusinessPlatform.Api.csproj --startup-project src/api/OpenBusinessPlatform.Api.csproj
cd src/api
dotnet build
```

The API writes generated build artifacts to `.artifacts/api` so local runs are isolated from stale `bin` or `obj` folders.

The backend persistence foundation uses EF Core with PostgreSQL `uuid`/C# `Guid` IDs, framework-lite audited entity base classes under `src/api/Domain/Common`, role/form permission tables for the first access model, and reusable CRUD primitives under `src/api/Application/Common`.

## Deployment Kit

The root `docker-compose.yml` is for local development. Reusable server deployment templates live in `deploy/`:

- `deploy/compose.yml`: generic server runtime for web, API, PostgreSQL, and Redis.
- `deploy/compose.proxy.yml`: optional Caddy reverse proxy for same-origin `/api` and React app traffic.
- `deploy/env/*.env.example`: safe templates only, with placeholders for private values.
- `deploy/github-actions/*.example`: inactive deploy workflow examples for private projects.

This core repo keeps the reusable configuration, while private projects should own real domains, secrets, active deploy workflows, and server paths. Docker image publishing is intentionally deferred for now; the deployment examples build from source on the server.

For local production-like testing, use `deploy/env/local.env.example` with `deploy/compose.local.example.yml`. That stack can run fully in Docker, or you can stop the Docker `web`/`api` services and run the frontend/API from the shell while Docker keeps PostgreSQL, Redis, and the proxy available. Apply EF Core migrations before the first bootstrap login against a fresh deployment database. See `deploy/README.md` for the full runbook.

Recent V1 finalization checks:

- `npm test`
- `npm run build`
- `dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj`
- `dotnet restore`
- `dotnet build`
- Compose API smoke tests for health, demo login, forms, published form rendering, records, record detail, and permission denials

## Local Services

Docker Compose exposes:

- PostgreSQL: `localhost:${POSTGRES_PORT:-55432}`
- Redis: `localhost:${REDIS_PORT:-6379}`

Development PostgreSQL connection:

```text
Host=localhost;Port=55432;Database=open_business_platform;Username=obp;Password=obp_dev_password
```

Apply EF Core migrations after PostgreSQL is running:

```bash
dotnet ef database update --project src/api/OpenBusinessPlatform.Api.csproj --startup-project src/api/OpenBusinessPlatform.Api.csproj
```

## Environment Configuration

The root `.env` file is for local development and is ignored by git. Use `.env.example` as the tracked template.

Important variables:

- `COMPOSE_PROJECT_NAME`: controls Docker Compose container, network, image, and volume naming so multiple clones can run at once.
- `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_PORT`: used by Docker Compose and the backend PostgreSQL connection. The default host port is `55432` to avoid collisions with a local PostgreSQL service on `5432`.
- `REDIS_HOST`, `REDIS_PORT`: used by Docker Compose and the backend Redis connection.
- `API_PORT`, `ASPNETCORE_URLS`: control the backend local URL for host-run API development.
- `VITE_APP_HOST`, `VITE_APP_PORT`, `VITE_API_BASE_URL`: control the Vite dev server and API proxy.
- `AUTH_COOKIE_NAME`: controls the auth cookie name. Use a different value per local clone to avoid browser cookie collisions on `localhost`.
- `AUTH_COOKIE_REQUIRE_SECURE`: controls whether non-development auth cookies must be marked secure. Keep this `true` for production HTTPS; use `false` only for temporary HTTP-only staging/testing.
- `VITE_APP_NAME`, `VITE_COMPANY_NAME`, `VITE_COMPANY_LOGO_URL`, and `BRAND_LOGO_TEXT`: control frontend branding defaults shown in the navbar, sidebar, settings, and login screen.
- `BOOTSTRAP_ADMIN_EMAIL`, `BOOTSTRAP_ADMIN_PASSWORD`: server-only bootstrap admin fallback credentials for local setup and access recovery.
- `DEFAULT_COMPANY_NAME`, `DEFAULT_COMPANY_LOGO_URL`: backend branding defaults until company settings move into the database.

Do not put secrets in `VITE_` or `BRAND_` variables. Vite exposes both prefixes to browser code.

## Run Two Clones

Use a different `.env` in each clone. Clone A can keep the defaults:

```env
COMPOSE_PROJECT_NAME=obp_a
POSTGRES_PORT=55432
REDIS_PORT=6379
API_PORT=5080
ASPNETCORE_URLS=http://localhost:5080
VITE_APP_HOST=127.0.0.1
VITE_APP_PORT=5174
VITE_API_BASE_URL=http://localhost:5080
AUTH_COOKIE_NAME=obp_a.auth
```

Clone B should use different host ports and a different cookie name:

```env
COMPOSE_PROJECT_NAME=obp_b
POSTGRES_PORT=55433
REDIS_PORT=6380
API_PORT=5081
ASPNETCORE_URLS=http://localhost:5081
VITE_APP_HOST=127.0.0.1
VITE_APP_PORT=5175
VITE_API_BASE_URL=http://localhost:5081
AUTH_COOKIE_NAME=obp_b.auth
```

Each clone can then run:

```bash
docker compose up -d
cd src/api && dotnet run
cd ../app && npm run dev
```

If you want Docker to build and run the API from that clone's code, use the optional API profile:

```bash
docker compose --profile api up -d --build
```

The API container uses the internal Compose service names `postgres` and `redis`, while host-run API development uses `localhost` and the published ports.

## Repository Structure

```text
src/
  api/        ASP.NET Core backend with minimal API modules
  app/        React frontend with app modules, shared UI, and theme playground

docs/         Product, architecture, data, API, security, and testing docs
tasks/        Step-by-step implementation tasks
prompts/      Reusable AI workflow prompts
```

## Documentation

Start here:

1. `docs/MASTER_PRD_FOR_AI.md`
2. `docs/PRD.md`
3. `docs/ARCHITECTURE.md`
4. `docs/TECH_STACK.md`
5. `docs/ROADMAP.md`
6. `tasks/v1/001-project-inventory-and-setup.md`

Work one task at a time. Do not build the full platform in one pass.

## Architecture Rules

- Use a modular monolith.
- Keep modules independent and practical.
- Do not add microservices in early versions.
- Do not add Native Federation yet.
- Do not add dynamic plugin loading yet.
- Do not use XYFlow for responsive form layout.
- Use XYFlow later only for workflow and approval diagrams.
