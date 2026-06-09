# V7 Start Here

This packet is the handoff for starting V7: Advanced Dashboards and Analytics.

## Current State

- V1 through V6 are complete for the current task lists.
- V6 task 007 added trigger email record PDF attachments from published print templates.
- The next implementation task is `tasks/v7/001-advanced-dashboard-analytics-foundation.md`.
- The working branch used for preparation was `dev`.
- Docker Compose services were stopped after verification cleanup.

## Read In This Order

1. `AGENTS.md`
2. `docs/MASTER_PRD_FOR_AI.md`
3. `docs/ROADMAP.md`
4. `docs/REPORTS_AND_PRINTING.md`
5. `docs/API_SPEC.md`
6. `docs/DATA_MODEL.md`
7. `tasks/v2/006-dashboard-summary-api.md`
8. `tasks/v2/007-chart-builder-lite.md`
9. `tasks/v2/008-dashboard-builder-lite.md`
10. `tasks/v7/README.md`
11. `tasks/v7/001-advanced-dashboard-analytics-foundation.md`
12. `docs/superpowers/specs/2026-06-08-v7-advanced-dashboard-analytics-design.md`
13. `docs/superpowers/plans/2026-06-08-v7-advanced-dashboard-analytics-foundation.md`

## V7 Task 001 Decision

Start backend-first. Add a typed dashboard analytics execution contract beside the existing V2 chart preview endpoint.

The planned endpoint is:

```txt
POST /api/dashboard/analytics/run
```

It should:

- Require authentication and `menu.dashboard`.
- Check source form `view` access.
- Apply the existing V3 record scope rules through `PermissionService`.
- Check source report `view` access when a saved list report source is supplied.
- Remove hidden fields from saved report filters/sorts/columns before execution.
- Reject direct references to hidden metric, group, date, or table fields with `403`.
- Return structured validation errors for unsupported widget types, metrics, fields, limits, and report sources.
- Leave `POST /api/forms/{formId}/chart-widgets/preview` working for the V2 chart builder and existing saved dashboards.

## Existing Code Surfaces

Backend dashboard code:

- `src/api/Modules/Dashboard/DashboardEndpoints.cs`
- `src/api/Modules/Dashboard/DashboardSummaryService.cs`
- `src/api/Modules/Dashboard/ChartContracts.cs`
- `src/api/Modules/Dashboard/ChartWidgetConfigValidator.cs`
- `src/api/Modules/Dashboard/ChartAggregationService.cs`
- `src/api/Modules/Dashboard/ChartAggregationEngine.cs`

Saved dashboard code:

- `src/api/Modules/Dashboards/DashboardDefinitionContracts.cs`
- `src/api/Modules/Dashboards/DashboardDefinitionValidator.cs`
- `src/api/Modules/Dashboards/DashboardDefinitionService.cs`
- `src/api/Modules/Dashboards/DashboardsEndpoints.cs`

Report and field metadata code reused by V7:

- `src/api/Modules/Forms/FormReportableFieldMetadata.cs`
- `src/api/Modules/Reports/ListReportConfigValidator.cs`
- `src/api/Modules/Reports/ListReportExecutionEngine.cs`
- `src/api/Modules/Identity/PermissionService.cs`

Frontend dashboard code:

- `src/app/src/features/dashboards/types.ts`
- `src/app/src/features/dashboards/api.ts`
- `src/app/src/features/dashboards/components/ChartWidgetPreview.tsx`
- `src/app/src/features/dashboards/pages/ChartBuilderPage.tsx`
- `src/app/src/features/dashboards/pages/DashboardsPage.tsx`

## Scope Boundaries For Task 001

Do:

- Add backend contracts, validation, service, endpoint, and focused tests.
- Add frontend API types/helper tests only if needed for contract coverage.
- Document the endpoint after implementation lands.
- Reuse existing aggregation and permission patterns.

Do not:

- Redesign dashboard UI.
- Add a charting or BI dependency.
- Add dashboard sharing/workspaces.
- Add cross-form joins, custom formulas, SQL, exports, or PDF output.
- Modify trigger, workflow, print, or report behavior except through shared permission/report filtering utilities needed by this task.

## Verification Commands

Run these before committing V7 Task 001:

```bash
dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj
dotnet build src/api/OpenBusinessPlatform.Api.csproj
cd src/app
npm test
npm run build
```

Run `git diff --check` before commit.

If API smoke is needed, start services with:

```bash
docker compose up -d
```

Then run the API host and check:

```bash
curl -i http://127.0.0.1:5080/health
```

Stop services with:

```bash
docker compose down
```
