# V2 Dashboard Builder Lite Design

## Status

Approved direction. This spec defines the focused V2 dashboard builder slice after chart widget previews are available.

## Context

V2 now has the data spine needed for saved dashboards:

- Forms provide reportable field metadata.
- Records store submitted values against form versions.
- Saved list reports can run against real permitted records.
- Dashboard summary metrics are database-backed.
- Chart widget previews can render number cards, bar charts, date trends, choice/status breakdowns, and table widgets from permitted form/report data.

The product direction is workspace first: users will eventually define a workspace, then dashboards, then charts/widgets inside that workspace. That workspace module is intentionally later. This task should finish the current V2 dashboard-builder slice without introducing workspace tables, tenant switching, public sharing, advanced permissions, XYFlow, triggers, workflows, or PDF templates.

## Goals

- Add saved dashboard definitions at `/dashboards`.
- Allow multiple named dashboards.
- Allow users to create, view, and update dashboard layouts.
- Reuse the existing chart widget config and preview path for dashboard widgets.
- Persist dashboard `config_json` and `layout_json` in PostgreSQL.
- Validate dashboard widgets against existing forms, reports, fields, metrics, and widget types on the backend.
- Keep the model ready for a later workspace parent without implementing workspace ownership now.

## Non-Goals

- No workspace table or workspace switching in this task.
- No drag-and-drop, resizable canvas, or XYFlow.
- No public sharing or dashboard-level ACLs.
- No advanced BI expressions, joins, or cross-workspace analytics.
- No CSV export or print layout work; those remain later V2 tasks.

## Recommended Approach

Build saved dashboards as their own module surface while keeping them connected to the existing V2 data spine.

The backend adds a `dashboards` table with:

- `id uuid`
- `name`
- `description`
- `config_json jsonb`
- `layout_json jsonb`
- audit/concurrency/delete metadata matching existing full-audited entities

The table should not include `workspace_id` yet. The service and DTO names should avoid global assumptions so a later workspace module can attach dashboards to a workspace with a migration and route adjustment.

Frontend dashboards live separately from the current system summary:

- `/dashboard` remains the current workspace/system overview.
- `/dashboards` becomes the saved dashboard builder and viewer.

## Dashboard Contract

Dashboard config stores widget definitions:

```json
{
  "schemaVersion": 1,
  "widgets": [
    {
      "id": "widget-1",
      "title": "Employees by department",
      "sourceFormId": "00000000-0000-0000-0000-000000000000",
      "chart": {
        "widgetType": "bar_chart",
        "metric": { "type": "count", "fieldId": null },
        "groupByFieldId": "department",
        "dateFieldId": null,
        "columns": [],
        "limit": 10,
        "reportId": null
      }
    }
  ]
}
```

Dashboard layout stores presentation choices:

```json
{
  "schemaVersion": 1,
  "widgets": [
    {
      "id": "widget-1",
      "width": "wide",
      "order": 1
    }
  ]
}
```

Supported widths are `small`, `medium`, `wide`, and `full`. The frontend maps them to a responsive CSS grid. Reordering is done with explicit up/down controls for this slice.

## Backend Design

Add a `Modules/Dashboards` backend module, separate from the existing `Modules/Dashboard` summary/chart preview module.

Endpoints:

- `GET /api/dashboards`
- `GET /api/dashboards/{dashboardId}`
- `POST /api/dashboards`
- `PUT /api/dashboards/{dashboardId}`

Authorization:

- List/detail require authenticated user plus `menu.dashboard`.
- Rendering widget previews through dashboard detail also requires the source form/report permissions already enforced by chart preview.
- Create/update require `reports.manage`. This matches the current V2 reporting-builder permission model and avoids new dashboard-specific permission constants in this slice.

Validation:

- Dashboard name is required.
- Config and layout schema versions must be supported.
- Every layout widget id must match exactly one config widget id.
- Widget ids must be unique.
- Widget widths must be supported.
- Every widget must reference an existing, non-deleted source form.
- If `reportId` is present, it must reference a non-deleted list report for that same form.
- Widget chart config validation must reuse `ChartWidgetConfigValidator`.
- Backend must validate source form/report existence and field compatibility before save, not only at preview time.

Audit:

- Create writes `dashboard_created`.
- Update writes `dashboard_updated`.

## Frontend Design

Add a saved dashboard page at `/dashboards` under `features/dashboards/pages`.

The page should include:

- Dashboard selector/list.
- Create dashboard form with name and optional description.
- Widget builder that reuses the existing chart config concepts: form, optional saved report, widget type, metric, grouping/date/columns, limit.
- Width selector with `small`, `medium`, `wide`, and `full`.
- Up/down reorder controls.
- Save/update action.
- Preview area rendering saved widgets through the same lightweight renderers used by Chart Builder Lite.

The current `/dashboard` page remains unchanged except for an optional quick action link to `/dashboards`.

Shared code should be extracted where it reduces real duplication:

- Chart preview rendering can move from `ChartBuilderPage.tsx` into a reusable dashboard component if needed.
- Dashboard API calls and types stay in `features/dashboards`.

## Data Flow

Create/update:

1. User creates or edits a dashboard in `/dashboards`.
2. Frontend builds dashboard config and layout JSON.
3. Backend checks authorization.
4. Backend validates dashboard name, config/layout consistency, source forms, source reports, and chart configs.
5. Backend saves JSONB and writes audit logs.
6. Frontend refreshes the saved dashboard list/detail.

View:

1. User opens `/dashboards`.
2. Frontend loads dashboard definitions.
3. User selects a dashboard.
4. Frontend previews each widget by calling the existing chart widget preview endpoint with the widget's source form and chart config.
5. Backend chart preview continues to enforce source permissions.
6. Frontend renders number, chart, trend, breakdown, and table widgets in the saved layout order.

## Error Handling

- Return `400` with validation errors for invalid dashboard config/layout.
- Return `403` for failed permission checks.
- Return `404` when a dashboard, source form, or source report does not exist.
- Return `409` when a source schema or saved report config can no longer support the widget.
- Frontend shows scoped alerts and keeps the user's current draft values where practical.

## Testing

Backend:

- Dashboard entity is mapped to a `dashboards` table.
- Config/layout JSON columns are JSONB.
- Dashboard validator rejects duplicate widget ids, layout/config mismatches, invalid widths, missing forms, mismatched reports, and invalid chart configs.
- Dashboard service DTOs and audit action strings are covered by the lightweight backend harness where practical.

Frontend:

- Dashboard API client maps list/detail/create/update requests and error responses.
- Dashboard layout helpers order widgets and map widths.
- Module route/navigation test includes `/dashboards` if navigation is added.

Build validation:

- `cd src/app && npm test`
- `cd src/app && npm run build`
- `dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj`
- `dotnet build src/api/OpenBusinessPlatform.Api.csproj`
- `dotnet ef migrations has-pending-model-changes --project src/api/OpenBusinessPlatform.Api.csproj --startup-project src/api/OpenBusinessPlatform.Api.csproj` after the migration is added.

## Documentation Updates

Update these files when implementation is complete:

- `docs/API_SPEC.md`
- `docs/MASTER_PRD_FOR_AI.md`
- `docs/REPORTS_AND_PRINTING.md`
- `docs/ROADMAP.md`
- `tasks/v2/008-dashboard-builder-lite.md`
- `tasks/v2/README.md`

Document the dashboards migration because this task adds a table.

## Acceptance Criteria Mapping

- Dashboard definitions can be saved with widget config and layout JSON through `dashboards.config_json` and `dashboards.layout_json`.
- Backend validation rejects unknown source forms/reports, fields, metrics, widget types, invalid widths, and mismatched layout/widget ids.
- Backend authorization is enforced on list/detail/create/update and existing widget preview permissions remain in force.
- Frontend can create, view, and update a dashboard layout at `/dashboards`.
- Database migration is documented.
- Tests cover backend validation/model shape and frontend API/layout helpers where practical.
- Documentation is updated if API contracts or task status change.
