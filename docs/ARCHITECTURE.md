# Architecture

## Overview

The platform is a modular monolith.

The current repository starts with:

- `src/api`: ASP.NET Core API host
- `src/app`: React frontend
- `docker-compose.yml`: PostgreSQL and Redis for local development
- `docs`: product and architecture documentation
- `tasks`: implementation tasks

The product should grow module by module rather than through a single large builder or early microservices.

Main modules:

- Forms
- Records
- Reports
- Permissions
- Triggers
- Workflows
- Printing
- Audit
- Notifications

Each module should have clear frontend, backend, and database responsibilities.

Do not add microservices, Native Federation, dynamic DLL plugin loading, or XYFlow in V1.

## Frontend Architecture

Current shared structure:

```txt
src/app/src/
  components/
    ui/
    layout/
  config/
  context/
  features/
    forms/
  layouts/
  pages/
  platform/
  modules/
  theme/
    pages/
    config/
    mockData.ts
  lib/
```

Future feature structure:

```txt
src/app/src/
  features/
    forms/
      components/
      hooks/
      types/
      api/
      utils/
    records/
    reports/
    permissions/
    triggers/
    workflows/
    printing/
    audit/
    notifications/
  lib/
  types/
```

Important frontend separation:

- `FormBuilder` edits draft form schema and layout.
- `FormRenderer` renders published form versions and previews.
- `RecordList` lists submitted records.
- `ReportBuilder` configures report definitions.
- `ReportViewer` displays configured reports.
- `/theme` demonstrates shared UI/layout components with sample data only.
- Real app pages should later use API/data services, not `/theme` mock data.

## Backend Architecture

Current backend structure:

```txt
src/api/
  Modules/
    Dashboard/
    Forms/
  Platform/
  Program.cs
```

Future backend module structure:

```txt
src/api/
  Modules/
    Forms/
    Records/
    Reports/
    Permissions/
    Triggers/
    Workflows/
    Printing/
    Audit/
    Notifications/
```

Endpoint/controller responsibility:

- Accept requests
- Validate model binding
- Call application service
- Return response

Application service responsibility:

- Business logic
- Permission checks
- Transactions
- Audit logs
- Trigger dispatch

Infrastructure responsibility:

- EF Core DbContext
- Repositories if used
- Email provider
- File storage
- PDF generation later

## Data Flow: Form Submission

1. User opens published form.
2. Frontend fetches published form version.
3. FormRenderer renders fields from schema/layout.
4. User submits values.
5. Backend validates values against form version schema.
6. Backend checks submit permission.
7. Backend creates record with form version ID.
8. Backend writes audit log.
9. Backend dispatches trigger event later.
10. Frontend shows success.

## Data Flow: Record List

1. User opens record list.
2. Frontend requests records for form/report.
3. Backend checks view permission.
4. Backend applies record-level filters.
5. Backend removes fields the user cannot see.
6. Backend returns paginated records.
7. Frontend displays table.

## Data Flow: Trigger Later

1. Record is created or updated.
2. Backend commits transaction.
3. Trigger dispatcher receives event.
4. Trigger engine loads enabled triggers for the form/event.
5. Conditions are evaluated.
6. Actions are executed.
7. Trigger logs are written.

## Key Design Decisions

- Store flexible schemas as JSONB.
- Store common query fields as relational columns.
- Use backend permission checks for every sensitive API.
- Keep responsive form layout grid-based, not canvas-based.
- Add XYFlow only for workflows later.
- Keep shared UI in `src/app/src/components`.
- Keep `/theme` as a playground, not as the owner of reusable UI.
