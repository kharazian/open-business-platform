# V7 Advanced Dashboard Analytics Design

## Context

V2 already has:

- A database-backed dashboard summary endpoint.
- A form-scoped chart widget preview endpoint.
- Saved dashboard definitions that persist widget config and layout JSON.
- Permission-aware chart preview execution over form records or optional saved list report filters.

V7 Task 001 should deepen that foundation without replacing it. This design was generated as an autonomous handoff for starting V7, per the user's instruction to proceed without asking for additional permission.

## Goal

Add typed backend analytics contracts and services that let dashboards request richer summary, trend, breakdown, and table data over permitted form or saved-report records.

## Recommended Approach

Add a new dashboard analytics execution surface beside the V2 chart preview endpoint:

```txt
POST /api/dashboard/analytics/run
```

This endpoint should accept a source form and optional saved list report, execute a typed analytics request, and return summary, series, or table-shaped results. The existing V2 endpoint stays in place:

```txt
POST /api/forms/{formId}/chart-widgets/preview
```

That keeps the current chart builder and saved dashboards stable while giving V7 a clearer dashboard-owned contract.

## Contract Shape

Request:

```json
{
  "widgetType": "breakdown",
  "source": {
    "formId": "00000000-0000-0000-0000-000000000000",
    "reportId": null
  },
  "metric": {
    "type": "count",
    "fieldId": null
  },
  "groupByFieldId": "status",
  "dateFieldId": null,
  "columns": [],
  "limit": 10
}
```

Supported `widgetType` values:

- `summary`
- `breakdown`
- `trend`
- `table`

Supported metric values:

- `count`
- `sum`
- `average`

Field rules:

- `sum` and `average` require a numeric reportable field.
- `breakdown` requires a status, department, select, radio, or other choice-groupable field.
- `trend` requires a date or datetime field.
- `table` accepts visible reportable field IDs and returns display-ready cells.
- `limit` is clamped or validated to a small bounded range; use `1` to `50` for parity with V2 chart previews.

Response:

```json
{
  "formId": "00000000-0000-0000-0000-000000000000",
  "formName": "Employee information",
  "reportId": null,
  "widgetType": "breakdown",
  "metric": {
    "type": "count",
    "fieldId": null
  },
  "series": [
    { "key": "active", "label": "Active", "value": 12 }
  ],
  "columns": [],
  "rows": [],
  "totalCount": 12
}
```

Table responses should use `columns` and `rows`; non-table responses should use `series`.

## Backend Architecture

Add these files in `src/api/Modules/Dashboard`:

- `DashboardAnalyticsContracts.cs`
- `DashboardAnalyticsRequestValidator.cs`
- `DashboardAnalyticsService.cs`

Modify these files:

- `DashboardEndpoints.cs`
- `Program.cs`
- `src/api.Tests/Program.cs`

Service flow:

1. Validate that the request and source form ID are present.
2. Require `menu.dashboard`.
3. Load the source form and its current schema.
4. Require source form `view` access.
5. Validate widget type, metric, fields, columns, and limit against reportable metadata.
6. Load field access rules through `PermissionService.GetFieldAccessAsync`.
7. Reject direct references to hidden metric, group, date, or table fields.
8. If `source.reportId` is present, require report `view` access and load the saved list report config.
9. Validate the source report config against the form schema.
10. Remove hidden fields from saved report columns, filters, and sorts before analytics execution.
11. Apply record access scope through `PermissionService.ApplyRecordAccessAsync` using form `view`.
12. Execute the analytics request.

For Task 001, the analytics service can map the new dashboard contract onto the existing `ChartAggregationEngine`:

- `summary` maps to `number_card`.
- `breakdown` maps to `choice_breakdown`.
- `trend` maps to `date_trend`.
- `table` maps to `table`.

That keeps the first V7 slice dependency-light and avoids duplicating aggregation math. Later V7 tasks can expand the engine if the dashboard builder needs richer chart families.

## Permissions And Hidden Fields

The endpoint must enforce permissions on the backend.

Permission behavior:

- User must be authenticated.
- User must have `menu.dashboard`.
- User must have source form `view`.
- Record set must be filtered by V3 record scope rules.
- Source report must require report `view` if `reportId` is supplied.

Hidden field behavior:

- Directly selected hidden metric, group, date, or table fields return `403`.
- Hidden fields are removed from saved source report columns, filters, and sorts before execution.
- Hidden fields are not returned in `columns`, `rows`, `series`, or cell dictionaries.

## Error Handling

Use structured validation errors:

```json
{
  "message": "Dashboard analytics request is invalid.",
  "errors": [
    {
      "path": "metric.fieldId",
      "code": "dashboard.analytics.metric.field_required",
      "message": "Sum and average metrics require a numeric field."
    }
  ]
}
```

Recommended status codes:

- `400` for invalid request shape, unsupported widget type, unsupported metric, invalid fields, or invalid limit.
- `403` for failed menu, form, report, or hidden-field checks.
- `404` for missing source form or saved report.
- `409` when a form or saved report schema/config cannot be used for analytics.

## Frontend Scope

Task 001 should keep frontend changes small:

- Add TypeScript request/response types.
- Add `runDashboardAnalytics` API helper.
- Add API helper tests.

Do not redesign `/dashboards` or the chart builder in Task 001. The dashboard builder upgrade belongs to V7 Task 002.

## Data Model

No new database table is required for Task 001.

Analytics execution is read-only and uses existing tables:

- `forms`
- `form_versions`
- `records`
- `reports`
- `dashboards`
- permission tables

Saved dashboards may later switch from `chart` config fields to richer `analytics` config fields, but that belongs to V7 Task 002.

## Test Plan

Add backend harness tests for:

- Typed request/response contracts.
- Validator support for summary, breakdown, trend, and table widgets.
- Count, sum, and average metrics.
- Invalid metric field, invalid grouping field, invalid date field, invalid columns, and invalid limits.
- Hidden field direct references returning a forbidden analytics error.
- Source report config access and sanitization using the same rules as V2 chart previews.
- Existing V2 chart preview and saved dashboard validation still passing.

Add frontend tests for:

- `runDashboardAnalytics` endpoint path, method, credentials, headers, and body.
- Error message extraction.

## Out Of Scope

- Dashboard UI redesign.
- Drag-and-drop dashboard layout changes.
- Workspace ownership or dashboard sharing.
- Cross-form joins.
- Custom formulas.
- Arbitrary SQL.
- Exporting dashboard analytics.
- PDF/dashboard print output.
- New charting dependencies.
