# Architecture

## Overview

The platform is a modular monolith.

The current repository starts with:

- `src/api`: ASP.NET Core minimal API host
- `src/app`: React frontend
- `docker-compose.yml`: PostgreSQL and Redis for local development
- `deploy/`: reusable server deployment templates for projects that consume the core
- `docs`: product and architecture documentation
- `tasks`: implementation tasks

The product should grow module by module rather than through a single large builder or early microservices.

Main modules:

- Tenants and Workspaces
- Forms
- Records
- Reports
- Dashboards
- Permissions
- Triggers
- Workflows
- Printing
- Audit
- Notifications
- Integrations
- Processing
- Creator Analysis

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
    creator-analysis/
    dashboards/
    forms/
    integrations/
    records/
    reports/
    triggers/
    workflows/
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
- Current app modules are dashboard, forms, users/access, reports, printing, triggers, workflows, integrations, compliance, notifications, settings, and profile.
- Real app routes are generated from modules in `App.tsx`; `/theme` routes are generated separately from the theme page config.

Current frontend shell/theme behavior:

- `AppThemeProvider` owns the real app appearance settings and persists them to the `appThemeSettings` localStorage key.
- Real app appearance settings include palette, color mode, density, app layout, radius, and shadow.
- `ThemeAppearanceProvider` owns the `/theme` playground appearance controls separately from the real app shell.
- `AppShell` is shared by the real app and `/theme`, with layout modes for topbar, sidebar, collapsed sidebar, hover-collapsed sidebar, hybrid, and minimal shells.
- `src/app/src/config/branding.ts` reads frontend branding from `VITE_APP_NAME`, `VITE_COMPANY_NAME`, `VITE_COMPANY_LOGO_URL`, and `BRAND_LOGO_TEXT`.
- `AuthContext` loads the signed-in user from `/api/auth/me`, stores effective permissions, and supports login/logout through cookie auth.
- The Forms feature currently includes persisted list/create, backend-owned draft metadata/schema editing, responsive layout settings, preview, publishing, and published-form submission.
- The Records feature currently includes form-scoped record lists, record detail, permission-aware reverse lookup panels, edit, soft-delete, and browser print views backed by the records API.
- The Users feature currently includes a Users & Access workspace for users, roles, role permissions, and per-form role access.
- The Workflows feature currently includes typed frontend workflow contracts, API helpers, draft/config helpers, and the `/workflows` management workspace for form-scoped definition list/create/edit/publish/enable/disable operations.

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
- Real app product pages should use API/data services as they mature; Forms, Users & Access, Records, V2 Reports, Dashboard summary, Charts, saved Dashboards, V4 Triggers, Notifications, and V5 Workflows now use real API-backed product surfaces. Settings/profile and `/theme` remain starter/sample surfaces.

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
    Dashboards/
    CreatorAnalysis/
    Forms/
    Identity/
    Integrations/
    Notifications/
    Printing/
    Processing/
    Records/
    Reports/
    Triggers/
    Workflows/
  Platform/
  Configuration/
  Program.cs
```

Current backend module behavior:

- `Program.cs` maps `/health` directly for API liveness, maps database-backed `/health/automation` separately for degraded automation readiness, and exposes token-protected payload-free `/metrics` aggregates.
- `Platform/IPlatformApiModule.cs` discovers API modules in the assembly and maps their endpoints.
- `Application/Common` contains DTO, paging, repository, and CRUD service base primitives for simple management resources.
- `Domain/Common` contains framework-lite entity base classes and capability interfaces for Guid IDs, auditing, soft delete, concurrency stamps, active status, extra JSON properties, and workspace ownership.
- `Infrastructure/Persistence/OpenBusinessPlatformDbContext.cs` maps PostgreSQL entities and centrally applies active-workspace query filters, automatic ownership assignment, cross-workspace write rejection, and immutable workspace ownership.
- `Infrastructure/Persistence/Migrations` contains EF Core migrations.
- `Modules/Dashboard` maps authenticated `GET /api/dashboard/summary`.
- `Modules/Identity` maps bootstrap-admin cookie authentication, local PostgreSQL user login, user management, role management, password reset, role permissions, and effective permission endpoints.
- `Modules/Identity` also owns the V9 OIDC SSO boundary: workspace-owned provider administration, public provider discovery, protected authorization-code/PKCE state, token validation, and external identity linking to existing active members.
- `Modules/Identity/AccessPolicyService.cs` adds typed enterprise deny guardrails after existing RBAC grants. Record status/ownership rules are composed into EF queries so list, report, export, dashboard, and print consumers retain database-side filtering.
- `Modules/Identity/RetentionService.cs` owns the non-destructive retention/legal-hold foundation and database-side dry-run evaluation.
- `Modules/Identity/AdministrativeBackupService.cs` creates bounded protected workspace snapshots and validation-only restore plans; it has no mutation path into business modules.
- `Modules/Identity/PermissionService.cs` centralizes the current global role permission and per-form role access checks.
- `Modules/Workspaces` owns tenant/workspace constants, signed request context, ownership guards, membership lifecycle services, per-request membership validation, and authenticated context/list/switch endpoints. Workspace-specific platform role claims are refreshed at login and switch time.
- `Modules/Forms` contains shared V1 form schema contracts and validation logic plus authenticated `GET /api/forms`, `POST /api/forms`, and `GET /api/forms/access-options` endpoints.
- `Modules/Records` contains record submit, list, detail, edit, and soft-delete endpoints with per-form permission checks, record value validation, concurrency checks for edits, and audit logging for mutations.
- `Modules/Reports` contains list report definition, execution, CSV export, one-hop relationship projection, and V10 typed operational action projection with config validation, bounded record-scope checks, report management/view permission checks, and report audit logging.
- `Modules/Processing` coordinates the existing import/export services through workspace-owned definitions and a bounded durable run queue. Its worker claims at most ten due schedules and five due runs per pass with five-minute leases, claim-token fencing, non-overlap, current-owner permission rechecks, and export-only retries. Abandoned export claims can be reclaimed, while abandoned CSV imports fail closed to prevent replay after possible partial mutation. Neutral recurrence calculation lives in `Application/Common` and is also used by triggers. Task 009 adds a separate fixed-catalog operational log, bounded health queries, daily technical retention, and replay-safe terminal failure delivery through the existing notification inbox. Audit and integration history remain separate authoritative modules.
- `Modules/CreatorAnalysis` owns the V10 request-scoped Creator-style compatibility boundary. It validates one bounded UTF-8 text upload, applies deterministic allowlisted scanning and pre-report secret suppression, returns only typed sanitized findings with `canImport: false`, and writes one aggregate-only audit event. It has no database entity, artifact, source persistence, external call, code execution, or apply path.
- `Modules/Triggers` contains the current V4 trigger definition, execution, retry, webhook action, and safe scheduled trigger foundation.
- `Modules/Workflows` contains the current V5 backend workflow definition foundation, typed config validation, management endpoints, immutable version publishing, and workflow history table foundation.
- `Configuration/DotEnv.cs` loads the nearest `.env` file without overriding existing environment variables.
- `Configuration/EnvironmentConfiguration.cs` derives connection strings, branding options, bootstrap admin options, `ASPNETCORE_URLS`, and local CORS defaults from environment variables.
- `Directory.Build.props` redirects API build output to `.artifacts/api`.
- In non-development environments, `Program.cs` applies forwarded headers before authentication and marks auth cookies as secure by default so the API can sit behind Caddy or Nginx TLS termination. Temporary HTTP-only staging can opt out through `AUTH_COOKIE_REQUIRE_SECURE=false`.

## Deployment Architecture

Local development stays in the root `docker-compose.yml`. Server deployment templates live in `deploy/` so private projects can copy or overlay them without putting private domains or secrets into the core repo.

The generic server runtime is:

```txt
proxy -> web
proxy -> api
api   -> postgres
api   -> redis
```

The frontend container serves the built React app through Nginx. The proxy keeps browser traffic same-origin by routing `/api/*`, `/health*`, and `/metrics` to the API while routing all other paths to the web container. Production metrics require a server-only bearer token. Staging and production should use separate Compose project names, volumes, cookie names, and env files.

Verified enabled custom hosts are resolved by API middleware before workspace membership enforcement. Anonymous requests may discover the mapped workspace; authenticated requests are rejected when the host mapping conflicts with the signed workspace claim. Proxy acceptance and TLS certificate issuance remain deployment responsibilities.

Compliance administration is a read model over authoritative workspace modules. It does not copy policy/retention/backup/domain state into a parallel compliance table. Audit review omits before/after payloads, sanitizes metadata, and bounds search/export work.

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
- Print/PDF generation services

## Data Flow: Form Submission

1. User opens published form.
2. Frontend fetches published form version.
3. FormRenderer renders fields from schema/layout.
4. File fields upload bounded content to protected pending attachment storage and retain only returned IDs.
5. User submits values.
6. Backend validates values and attachment ownership against the immutable form version.
7. Backend checks submit permission.
8. Backend creates the record and conditionally claims pending attachments in one transaction.
9. Backend writes audit logs.
10. Backend dispatches trigger events later.
11. Frontend shows success.

## Data Flow: Record List

1. User opens record list.
2. Frontend requests records for form/report.
3. Backend checks view permission.
4. Backend applies record-level filters.
5. Backend removes fields the user cannot see.
6. Backend returns paginated records.
7. Frontend displays table.

## Data Flow: One-Hop Report Relationships

1. The report builder requests the root form's permission-filtered field catalog.
2. The report module expands visible `recordLookup` fields into canonical `{lookupFieldId}.{targetFieldId}` options for viewable target forms and visible target fields.
3. Saved schema-version-1 report configs keep root IDs unchanged and may reference exactly one dotted lookup hop.
4. Report execution loads permission-scoped root records, reads stored lookup UUIDs, and queries only non-deleted target records allowed by the caller's target-form and record scopes.
5. Missing, inaccessible, hidden, or stale related values become empty cells; raw target IDs are never used as fallback display values.
6. The shared report execution path applies filters, search, sort, display, CSV, and print/PDF behavior to the terminal field's typed value.

## Data Flow: Typed Report Actions

1. A saved report stores ordered allowlisted report and row action definitions in its existing config JSONB.
2. Report execution resolves enabled report actions against form/report permissions and row actions against the concrete page's record scopes and workspace policies.
3. The API returns only safe IDs, types, labels, and delete confirmation text; routes, URLs, scripts, and request payloads are not persisted.
4. The frontend renders the projected order and invokes existing trusted create, print, export, view, edit, or delete flows.
5. Every destination endpoint reauthorizes the operation and retains its existing audit, concurrency, trigger/outbox, relationship, and soft-delete semantics.

## Data Flow: Related-Record Workspace

1. Record detail loads the selected target record independently from its related workspace.
2. The records module discovers historical and current source form-version lookup definitions that target the selected record's form.
3. Backend form and hidden-field checks remove inaccessible panel definitions before metadata or counts are returned.
4. Each panel queries indexed canonical edges plus a case-insensitive JSONB scalar compatibility path, then validates every candidate against its immutable source schema and stored lookup value.
5. Existing source record scopes and deny policies filter counts and rows before pagination.
6. The row API returns at most five visible display-only cells plus the accessible source record ID, status, and creation time; unresolved lookup/file UUIDs are never fallback values.
7. The frontend loads, retries, and paginates every panel independently and keeps related data out of edit and print output.

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
- Store attachment metadata and bounded private PostgreSQL content separately from record JSON. Access content through a storage interface, inspect it through a scanner interface, and authorize every download against current record/field access.
- Treat lookup UUIDs in immutable-version record JSON as source values and `record_relationships` as derived integrity/index metadata. Synchronize edges transactionally, restrict referenced target deletion, and never expose a generic edge mutation API.
- Keep one-hop report traversal inside the reports module. Dotted report field keys are read-only projections over stored lookup values, not a graph API or relationship mutation mechanism.
- Keep reverse related-record discovery inside the records module. Use separate bounded discovery/row endpoints and display-only projections; do not expand the primary record payload or expose generic graph traversal.
- Use backend permission checks for every sensitive API.
- Keep responsive form layout grid-based, not canvas-based.
- Use XYFlow only for workflow visual authoring.
- Keep shared UI in `src/app/src/components`.
- Keep `/theme` as a playground, not as the owner of reusable UI.
- Store workspace branding as backend-owned workspace configuration. The real app and slug-selected login page consume a safe display projection; per-user appearance settings remain local and `/theme` remains an independent playground.
- Resolve localization in three layers: per-user override, workspace default, then platform fallback. Frontend modules consume shared `Intl` helpers instead of embedding locale/timezone decisions in individual components.
- Keep dashboard templates as dependency-free frontend definitions registered through the dashboard template catalog. Templates own source-slot references, sections, widget recipes, and default layout; they never own environment-specific form/report IDs.
- Template instantiation validates and binds permitted sources, generates fresh section/widget IDs, and creates a normal saved draft through `/api/dashboards`. Saved dashboards remain independent snapshots and the normal backend analytics/publishing paths remain the only execution and persistence boundaries.
- Treat optional dashboard template provenance as informational JSONB metadata. It never grants source access and never causes an automatic template upgrade.
- Keep specialized dashboard visuals behind a frontend adapter registry with bounded scalar settings. Adapters do not execute SQL, join form sources, or imply that missing Operations, Finance, or HSE domain modules exist.
- Bound saved dashboards to 16 sections, 48 widgets, 16 widgets per section, and eight filters. Section icons come from a shared allowlist.

## Data Flow: Dashboard Template Creation

1. The builder shows Blank dashboard plus registered templates.
2. The user explicitly selects permitted form/report bindings for each template source slot.
3. The frontend validates template structure, GUID syntax, and reportable field capabilities.
4. Instantiation deep-copies recipes, generates runtime IDs, and records non-authoritative template provenance.
5. The existing saved-dashboard API validates the complete definition against current forms/reports and saves it as a draft.
6. Analytics requests independently recheck form/report access, record scopes, and hidden fields.

Viewer filter selections are temporary runtime state. `Apply` sends bounded field/value or date-bound filters with each compatible widget request; `Reset all` clears them. Saved definitions retain only safe labels, field IDs, options, source IDs, and optional widget targets. The backend remains authoritative for field visibility and applies inclusive-start/exclusive-end date semantics.
