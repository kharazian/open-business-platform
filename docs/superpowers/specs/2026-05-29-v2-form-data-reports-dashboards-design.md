# V2 Form Data, Reports, Dashboards, and Charts Design

## Status

Approved direction. This spec adjusts V2 from "reports and better printing only" to a report-first data spine that also supports real dashboards and chart widgets.

## Context

V1 is complete for the current repository. Forms can be created, drafted, published, submitted, listed, edited, printed, permission-checked, audited, and seeded with demo data.

V2 has already started with saved list report definitions. Users with report management and form manage access can save list report definitions with selected columns, one UI filter, and one UI sort. The backend stores report config as JSONB, validates config against the form schema and supported system fields, checks permissions, and writes report audit entries.

The wider platform direction is an engine-based business app builder:

- Forms define fields, layout, validation, draft editing, published versions, and form detail/open/edit surfaces.
- Records store submitted values, preserve submitted form versions, support detail/edit views, and produce single-record print output.
- Reports show form records in table format and become the shared query path for report viewing, table widgets, export, and report print.
- Validation rules, triggers, workflows, schedules, notifications, API calls, record creation/update actions, and future print/PDF actions should build on the same normalized form/record/report data spine.

The new V2 focus is:

1. Make form data reliable for reporting.
2. Run saved reports against real record data.
3. Use the same report/query path to power dashboards and charts.
4. Add a small saved dashboard builder after the data path is solid.
5. Finish CSV export and clean print layouts.

## Goals

- Keep forms as the source of field definitions and record values.
- Keep report definitions separate from form schemas.
- Add backend report execution with filters, search, sorting, pagination, and permission checks.
- Add backend aggregation support for dashboard cards and simple charts.
- Replace starter dashboard sample data with real database-backed summaries.
- Add chart/dashboard builder primitives without introducing workflow, automation, XYFlow, PDF templates, or advanced permissions.
- Keep the frontend split between FormBuilder, FormRenderer, ReportBuilder, ReportViewer, ChartBuilder, and DashboardBuilder.

## Non-Goals

- No workflow or approval builder.
- No XYFlow in V2.
- No trigger engine.
- No scheduled trigger runner.
- No general action engine.
- No custom PDF templates.
- No advanced record/field-level permission model beyond the current backend checks.
- No full BI engine, expression language, joins across unrelated forms, or cross-workspace analytics.

## Recommended Approach

Use a report-first data spine.

Report execution becomes the shared backend path for list reports, dashboard table widgets, CSV export, and print. Dashboard chart widgets can either point at a saved report or use the same form/query primitives with an aggregation config.

This avoids separate query logic for reports and dashboards.

This also keeps later automation reachable. Validation rules, triggers, workflows, scheduled jobs, and action execution should consume the same reportable field metadata and record-value normalization rather than inventing separate ways to understand form data.

## V2 Task Sequence

1. Form data readiness.
2. Report viewer and execution.
3. Dashboard summary API.
4. Chart builder lite.
5. Dashboard builder lite.
6. CSV export.
7. Clean print layouts.

## Architecture

### Frontend

New or expanded feature areas:

- `features/reports`
  - `ReportBuilder`: saved definition creation and editing.
  - `ReportViewer`: runnable list reports over record data.
  - Report API client/types/helpers.
- `features/dashboards`
  - Dashboard summary API client.
  - Dashboard widget types.
  - ChartBuilder lite.
  - DashboardBuilder lite.
- `features/forms`
  - Reportable field helpers and form-data readiness helpers.

Shared UI stays in `components/ui`. Shared layout stays in `components/layout`. The `/theme` playground remains sample-data only.

### Backend

Expanded modules:

- `Modules/Reports`
  - Load saved report definitions.
  - Execute list reports against permitted records.
  - Apply filters, search, sorting, and pagination.
  - Return display-ready cells and raw values needed for export.
  - Export CSV from permitted report rows.
- `Modules/Dashboard`
  - Replace fixed starter metrics with real counts.
  - Add summary endpoints for number cards and chart-ready aggregates.
- Later in V2, optional `Modules/Dashboards`
  - Persist saved dashboard definitions with JSONB layout/config.

### Database

Current V2 can keep using the existing `reports` table for saved list report definitions.

Add a `dashboards` table only when saved custom dashboards are implemented:

- id uuid
- name
- config_json JSONB
- layout_json JSONB nullable
- created_by_id
- updated_by_id
- created_at
- updated_at
- is_deleted
- concurrency_stamp

Until then, the real dashboard can be database-backed without a new table.

## Data Flow

### Report Viewer

1. User opens a saved report.
2. Frontend requests report execution with page, page size, search, and optional runtime filters.
3. Backend checks `menu.reports` and form view access.
4. Backend loads the saved report definition and the form schema.
5. Backend queries records for the form.
6. Backend applies config filters, runtime search, sorting, and pagination.
7. Backend returns columns, rows, pagination metadata, and report metadata.

### Dashboard Summary

1. User opens dashboard.
2. Frontend requests dashboard summary.
3. Backend checks authentication and dashboard menu permission.
4. Backend returns real counts for users, forms, reports, records, and audit activity.
5. Frontend renders number cards and lightweight charts.

### Chart Widget

1. Builder selects a source form or saved report.
2. Builder selects metric: count, sum, average, min, or max where field types support it.
3. Builder selects grouping: status, choice field, created date bucket, or department where available.
4. Backend validates the config against the form schema and permissions.
5. Backend returns chart-ready labels and values.

## Error Handling

- Return 403 for failed permission checks.
- Return 404 when a form, report, or dashboard is missing.
- Return 400 with validation errors when report/chart/dashboard config references unknown fields or unsupported operators.
- Return 409 when a report cannot run because the form schema is unavailable.
- Frontend pages show scoped alerts and preserve user selections where practical.

## Testing

Frontend:

- Report execution API client parsing.
- Report field and column helpers.
- Dashboard/chart config helper tests.
- Module route/navigation tests if new modules are added.

Backend:

- Report config validation additions.
- Report execution DTO and query helper behavior.
- Dashboard summary DTOs and permission constants.
- EF model checks if a dashboards table is added.

Build validation:

- `cd src/app && npm test`
- `cd src/app && npm run build`
- `dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj`
- `dotnet build src/api/OpenBusinessPlatform.Api.csproj`
- `dotnet ef migrations has-pending-model-changes --project src/api/OpenBusinessPlatform.Api.csproj --startup-project src/api/OpenBusinessPlatform.Api.csproj` when persistence changes.

## Acceptance Criteria

- Saved list reports can be run against real permitted records.
- Report viewer honors backend filters, search, sort, pagination, and form permissions.
- Dashboard starter data is replaced by backend-owned real metrics.
- Chart widgets can render at least number cards, bar charts, date trends, and table widgets from permitted data.
- Saved custom dashboards are available by the end of V2 if the dashboard builder slice is included.
- CSV export and clean print layouts use the same permission-checked report data path.
- Documentation and task files reflect the adjusted V2 scope.
