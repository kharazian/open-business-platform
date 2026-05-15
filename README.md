# Open Business Platform

Open Business Platform is an open-source modular low-code platform for building internal business applications.

The first product direction is a business form platform: users can create responsive forms, publish versioned forms, collect records, view records and reports, control access, print data, and later automate work with triggers and approvals.

## Current Stack

- Backend: ASP.NET Core targeting .NET 10
- Frontend: React, Vite, TypeScript, Tailwind CSS
- Database: PostgreSQL
- Cache/queue foundation: Redis
- Local orchestration: Docker Compose

## Current Skeleton

The repository currently includes:

- ASP.NET Core API in `src/api`
- React frontend in `src/app`
- Shared frontend UI/layout components
- Main app routes for dashboard, users, reports, settings, and profile
- `/theme` playground for sample-data UI, layout, and component demos
- Docker Compose services for PostgreSQL and Redis
- API health endpoint at `http://localhost:5080/health`

## Product Direction

V1 focuses on:

- Form list and form draft creation
- Basic field builder
- Responsive layout builder
- Form preview and publish
- Record submission and record management
- Basic permissions
- Browser printing
- Audit logs
- Seed/demo data

Future versions add report builder, cleaner printing, advanced permissions, triggers, workflows, PDF templates, dashboards, integrations, and enterprise features.

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

Run the frontend in another terminal:

```bash
cd src/app
npm install
npm run dev -- --host 127.0.0.1 --port 5174
```

Open:

```text
http://localhost:5174
```

Useful frontend routes:

- `/`
- `/dashboard`
- `/settings`
- `/profile`
- `/theme`
- `/theme/components`
- `/theme/layouts`

## Build

Frontend:

```bash
cd src/app
npm test
npm run build
```

Backend:

```bash
cd src/api
dotnet build
```

The API writes generated build artifacts to `.artifacts/api` so local runs are isolated from stale `bin` or `obj` folders.

## Local Services

Docker Compose exposes:

- PostgreSQL: `localhost:${POSTGRES_PORT:-5432}`
- Redis: `localhost:${REDIS_PORT:-6379}`

Development PostgreSQL connection:

```text
Host=localhost;Port=5432;Database=open_business_platform;Username=obp;Password=obp_dev_password
```

## Environment Configuration

The root `.env` file is for local development and is ignored by git. Use `.env.example` as the tracked template.

Important variables:

- `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_PORT`: used by Docker Compose and the backend PostgreSQL connection.
- `REDIS_HOST`, `REDIS_PORT`: used by Docker Compose and the backend Redis connection.
- `API_PORT`, `ASPNETCORE_URLS`: control the backend local URL.
- `VITE_APP_PORT`, `VITE_API_BASE_URL`: control the Vite dev server and API proxy.
- `BRAND_LOGO_TEXT`: controls the compact logo text shown in the navbar, sidebar, and login screen.
- `BOOTSTRAP_ADMIN_EMAIL`, `BOOTSTRAP_ADMIN_PASSWORD`: server-only bootstrap admin defaults for the future user seed flow.
- `DEFAULT_COMPANY_NAME`, `DEFAULT_COMPANY_LOGO_URL`: branding defaults until company settings move into the database.

Do not put secrets in `VITE_` or `BRAND_` variables. Vite exposes both prefixes to browser code.

## Repository Structure

```text
src/
  api/        ASP.NET Core backend
  app/        React frontend

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
