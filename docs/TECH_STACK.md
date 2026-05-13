# Technical Stack

## Current Stack

- Frontend: React, React Router, Vite, TypeScript, Tailwind CSS, lucide-react
- Backend: ASP.NET Core targeting .NET 10
- Database: PostgreSQL 16 through Docker Compose
- Cache/queue foundation: Redis 7 through Docker Compose
- Package manager: npm
- Local frontend port: `5174`
- Local backend URL: `http://localhost:5080`

## Recommended Frontend Additions

Use these only when a task needs them:

- TypeScript
- Tailwind CSS theme tokens already exist
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
- EF Core
- Npgsql PostgreSQL provider
- FluentValidation or built-in validation
- ASP.NET Core Identity, JWT, or existing auth provider
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
- `created_by`
- `created_at`
- `updated_at`

## Recommended Build Commands

Frontend:

```bash
cd src/app
npm install
npm run build
```

Backend:

```bash
cd src/api
dotnet restore
dotnet build
```

Run local services:

```bash
cp .env.example .env
docker compose up -d
```

Run development servers:

```bash
cd src/api
dotnet run
```

```bash
cd src/app
npm run dev -- --host 127.0.0.1 --port 5174
```

Test commands should be added once test projects are introduced.

## Local Environment

Local development uses a root `.env` file copied from `.env.example`.

- Docker Compose reads `POSTGRES_*` and `REDIS_PORT`.
- The ASP.NET Core API loads the nearest `.env` file for local development, derives connection strings from `POSTGRES_*` and `REDIS_*`, and maps bootstrap admin variables into `BootstrapAdmin` options.
- Vite reads the root env file through `envDir` and uses `VITE_APP_PORT`, `VITE_API_BASE_URL`, and the non-secret `BRAND_LOGO_TEXT` value.
- Admin credentials must remain server-only. Do not add admin password values to `VITE_` variables.

## Important Library Decision

Do not add XYFlow in V1.

Add `@xyflow/react` only when implementing the workflow/approval builder in V5 or later.
