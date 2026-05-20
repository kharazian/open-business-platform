# Architecture

## Overview

The platform is a modular monolith.

The current repository starts with:

- `src/api`: ASP.NET Core minimal API host
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
    users/
  layouts/
  pages/
  modules/
  platform/
  theme/
    pages/
    config/
    mockData.ts
  lib/
```

Current frontend module registry:

- `src/app/src/modules/index.ts` exports the app modules.
- Each file under `src/app/src/modules/*/module.tsx` implements `PlatformModule`.
- `src/app/src/platform/moduleRegistry.ts` sorts modules by `order`, exposes routes, derives navigation, and filters navigation by effective permissions.
- Current app modules are dashboard, forms, users/access, reports, settings, and profile.
- Real app routes are generated from modules in `App.tsx`; `/theme` routes are generated separately from the theme page config.

Current frontend shell/theme behavior:

- `AppThemeProvider` owns the real app appearance settings and persists them to the `appThemeSettings` localStorage key.
- Real app appearance settings include palette, color mode, density, app layout, radius, and shadow.
- `ThemeAppearanceProvider` owns the `/theme` playground appearance controls separately from the real app shell.
- `AppShell` is shared by the real app and `/theme`, with layout modes for topbar, sidebar, collapsed sidebar, hover-collapsed sidebar, hybrid, and minimal shells.
- `src/app/src/config/branding.ts` reads frontend branding from `VITE_APP_NAME`, `VITE_COMPANY_NAME`, `VITE_COMPANY_LOGO_URL`, and `BRAND_LOGO_TEXT`.
- `AuthContext` loads the signed-in user from `/api/auth/me`, stores effective permissions, and supports login/logout through cookie auth.
- The Forms feature currently includes a persisted list/create page plus a local field-builder page that stores draft schemas in browser `localStorage`.
- The Users feature currently includes a Users & Access workspace for users, roles, role permissions, and per-form role access.

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
- Real app product pages should use API/data services as they mature; Forms and Users & Access already do, while dashboard/reports remain starter surfaces.

## Backend Architecture

Current backend structure:

```txt
src/api/
  Application/
    Common/
  Domain/
    Common/
    Entities/
  Infrastructure/
    Persistence/
  Modules/
    Dashboard/
    Forms/
    Identity/
  Platform/
  Configuration/
  Program.cs
```

Current backend module behavior:

- `Program.cs` maps `/health` directly.
- `Platform/IPlatformApiModule.cs` discovers API modules in the assembly and maps their endpoints.
- `Application/Common` contains DTO, paging, repository, and CRUD service base primitives for simple management resources.
- `Domain/Common` contains framework-lite entity base classes and capability interfaces for Guid IDs, auditing, soft delete, concurrency stamps, active status, and extra JSON properties.
- `Infrastructure/Persistence/OpenBusinessPlatformDbContext.cs` maps the V1 PostgreSQL tables for users, roles, role permissions, form permissions, departments, forms, form versions, records, and audit logs.
- `Infrastructure/Persistence/Migrations` contains EF Core migrations.
- `Modules/Dashboard` maps authenticated `GET /api/dashboard/summary`.
- `Modules/Identity` maps bootstrap-admin cookie authentication, local PostgreSQL user login, user management, role management, password reset, role permissions, and effective permission endpoints.
- `Modules/Identity/PermissionService.cs` centralizes the current global role permission and per-form role access checks.
- `Modules/Forms` contains shared V1 form schema contracts and validation logic plus authenticated `GET /api/forms`, `POST /api/forms`, and `GET /api/forms/access-options` endpoints.
- `Configuration/DotEnv.cs` loads the nearest `.env` file without overriding existing environment variables.
- `Configuration/EnvironmentConfiguration.cs` derives connection strings, branding options, bootstrap admin options, `ASPNETCORE_URLS`, and local CORS defaults from environment variables.
- `Directory.Build.props` redirects API build output to `.artifacts/api`.

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
- Use generic CRUD base services only for straightforward admin/config entities.
- Use custom services for form publishing, record submission, permission evaluation, triggers, workflows, and audit writing.

Infrastructure responsibility:

- EF Core DbContext
- EF repository implementation for `IRepository<TEntity, TKey>`
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
