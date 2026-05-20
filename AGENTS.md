# AGENTS.md

## Project

This project is a modular low-code business form platform.

The current stack is:

- Frontend: React, React Router, Vite, TypeScript, Tailwind CSS, lucide-react
- Backend: ASP.NET Core minimal APIs targeting .NET 10
- Database: PostgreSQL

The platform will include:

- Responsive form builder
- Record engine
- Report builder
- Permission engine
- Trigger engine
- Workflow/approval engine
- Print/PDF engine
- Audit log engine
- Notification engine

## Highest-Level Architecture Rules

- Keep the frontend, backend, and database responsibilities clear.
- Do not build one giant visual builder for everything.
- Use a custom React + Tailwind-style responsive layout builder for form layout.
- Do not use XYFlow for responsive form design.
- Use XYFlow only later for workflow, approval flow, and advanced automation diagrams.
- Keep forms, records, reports, permissions, triggers, workflows, printing, audit, and notifications as separate modules.
- Keep schemas and business rules separate from UI components.
- Enforce permissions on the backend, not only in React.
- Every published form must be versioned.
- Every record must store the form version used at submission time.
- Every sensitive action should have an audit log.

## Frontend Rules

- Work in `src/app`.
- Use React with TypeScript if the existing project supports TypeScript.
- Prefer strict types and avoid `any`.
- Keep shared UI components in `src/app/src/components/ui`.
- Keep shared layout components in `src/app/src/components/layout`.
- Keep real app pages in `src/app/src/pages`.
- Keep `/theme` as a sample-data playground and design-system demo.
- Use shared components for both real app pages and `/theme` pages.
- Do not let `/theme` own reusable UI primitives.
- Keep components small and reusable.
- Keep feature code grouped by module.
- Keep business logic outside React components where possible.
- Use forms schema to render forms; do not hardcode generated forms.
- The form renderer should be separate from the form builder.
- The report viewer should be separate from the report builder.
- Permission checks can hide UI, but backend authorization is still required.

Recommended frontend structure:

```txt
src/app/src/
  components/
  context/
  config/
  layouts/
  pages/
  features/
    forms/
    records/
    reports/
    permissions/
    triggers/
    workflows/
    printing/
    audit/
    notifications/
  theme/
  lib/
  types/
```

## Backend Rules

- Use ASP.NET Core controllers or minimal APIs according to the current project style.
- Keep business logic in services, not controllers.
- Keep database access behind repositories or EF Core DbContext patterns according to the project style.
- Use DTOs for API contracts.
- Validate requests on the backend.
- Use centralized authorization/permission checks.
- Add audit logs for record changes, permission changes, report exports, prints, and trigger executions.
- Use database transactions for operations that modify multiple entities.
- Do not expose hidden field values to unauthorized users.

Recommended backend structure:

```txt
src/
  Api/
    Controllers/
    Middleware/
  Application/
    Forms/
    Records/
    Reports/
    Permissions/
    Triggers/
    Workflows/
    Printing/
    Audit/
    Notifications/
  Domain/
    Entities/
    Enums/
    ValueObjects/
  Infrastructure/
    Persistence/
    Services/
```

Adapt this structure to the existing solution if it already has conventions.

## Database Rules

- PostgreSQL is the source of truth.
- Use EF Core migrations if the backend already uses EF Core.
- Prefer normalized core entities plus JSONB for flexible form schemas and record values.
- Use JSONB for form schema, layout config, report config, trigger config, and record values where appropriate.
- Use indexes for form IDs, record status, created date, owner, department, and JSONB fields that are frequently queried.
- Do not change schema without documenting the migration.

## Commands

Start local services:

```bash
docker compose up -d
```

Frontend:

```bash
cd src/app
npm install
npm run dev
npm test
npm run build
```

The frontend package requires Node.js `>=20.19.0`.

Backend:

```bash
dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj
cd src/api
dotnet restore
dotnet build
dotnet run
```

The backend test project is currently a lightweight executable test harness. Replace or supplement it with `dotnet test` when a formal test project is introduced.

## Testing Expectations

Add tests where practical.

Prioritize tests for:

- Form schema validation
- Record creation
- Permission checks
- Trigger condition evaluation
- Report filters
- API authorization

Recommended test types:

- Frontend unit/component tests
- Backend unit tests
- Backend integration tests
- E2E tests later

## Codex / AI Work Rules

When implementing a task:

1. Read `docs/MASTER_PRD_FOR_AI.md` first.
2. Read the relevant docs file.
3. Read the specific task file.
4. Implement only the requested task.
5. Do not add unrelated features.
6. Do not introduce large dependencies without explaining why.
7. Do not use XYFlow before workflow tasks.
8. Update documentation if architecture or commands change.
9. Run available tests/builds.
10. Summarize files changed, tests run, risks, and follow-up work.
