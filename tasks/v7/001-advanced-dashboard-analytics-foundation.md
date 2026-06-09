# V7 Task 001: Advanced Dashboard Analytics Foundation

## Goal

Add backend analytics contracts and services that let dashboards request richer summary, trend, and breakdown data over permitted form/report records.

## Context

Read:

- `docs/MASTER_PRD_FOR_AI.md`
- `docs/ROADMAP.md`
- `docs/REPORTS_AND_PRINTING.md`
- `docs/API_SPEC.md`
- `docs/DATA_MODEL.md`
- `tasks/v2/006-dashboard-summary-api.md`
- `tasks/v2/007-chart-builder-lite.md`
- `tasks/v2/008-dashboard-builder-lite.md`
- `docs/V7_START_HERE.md`
- `docs/superpowers/specs/2026-06-08-v7-advanced-dashboard-analytics-design.md`
- `docs/superpowers/plans/2026-06-08-v7-advanced-dashboard-analytics-foundation.md`
- `AGENTS.md`

## Requirements

- Extend dashboard analytics without replacing the current V2 dashboard summary, chart preview, or saved dashboard definitions.
- Add typed backend contracts for advanced summary widgets, date trends, grouped breakdowns, and dashboard table slices.
- Reuse existing form/report permission checks, record access scopes, saved report filters, and hidden-field filtering.
- Support one source form and optional saved list report per analytics widget.
- Keep the first implementation dependency-light; do not add a charting or BI dependency in this task.
- Add audit or usage logs only if an existing endpoint pattern already requires them.
- Keep frontend changes limited to API types/helpers/tests if needed for contract coverage; defer broad UI work to the next V7 task.
- Document any new endpoint, config field, or response contract.

## Acceptance Criteria

- [ ] Advanced dashboard analytics contracts are typed on the backend.
- [ ] Analytics execution supports count, sum, average, status/choice breakdowns, date trends, and table slices over permitted records.
- [ ] Optional saved report sources reuse report filters and report view permissions.
- [ ] Hidden fields cannot be selected or returned by analytics responses.
- [ ] Backend validation returns structured errors for unsupported fields, metrics, grouping, date fields, limits, or report sources.
- [ ] Existing V2 dashboard summary, chart preview, and saved dashboards still work.
- [ ] Tests are added where practical.
- [ ] Documentation is updated if contracts change.
- [ ] Relevant build/test commands are run.

## Out of Scope

- Dashboard UI redesign.
- Drag-and-drop dashboard layout.
- Workspace ownership or dashboard sharing.
- Cross-form joins.
- Custom formulas or arbitrary SQL.
- Exporting dashboard data.
- PDF/dashboard print output.

## Notes

- Implement only this task.
- Prefer focused backend tests in the lightweight harness before adding implementation code.
- Keep the analytics service small enough that later widget/UI tasks can reuse it without duplicating aggregation logic.
