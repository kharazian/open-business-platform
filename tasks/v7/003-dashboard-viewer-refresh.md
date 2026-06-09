# V7 Task 003: Dashboard Viewer Refresh

## Goal

Refresh the dashboard viewer so V7 analytics widgets are easier to scan in day-to-day business operations.

## Context

Read:

- `docs/MASTER_PRD_FOR_AI.md`
- `docs/ROADMAP.md`
- `docs/REPORTS_AND_PRINTING.md`
- `docs/API_SPEC.md`
- `docs/V7_START_HERE.md`
- `tasks/v7/001-advanced-dashboard-analytics-foundation.md`
- `tasks/v7/002-dashboard-widget-builder-upgrade.md`
- `AGENTS.md`

## Requirements

- Render V7 summary, breakdown, trend, and table widgets in the saved dashboard viewer.
- Preserve existing saved dashboard viewer behavior.
- Keep the layout dense, readable, responsive, and operational.
- Improve loading, empty, permission-denied, and stale-config states.
- Avoid a large chart dependency unless the current lightweight renderers cannot meet the requirement.
- Keep cards only for individual widgets; do not nest page sections inside cards.

## Acceptance Criteria

- [ ] V7 analytics widgets render in the dashboard viewer.
- [ ] Existing V2 widgets continue to render.
- [ ] Empty and error states are clear per widget.
- [ ] Responsive layouts remain stable on mobile and desktop.
- [ ] Text does not overlap or overflow controls/widgets.
- [ ] Tests are added where practical.
- [ ] Documentation is updated if viewer behavior changes.
- [ ] Relevant build/test commands are run.

## Out of Scope

- Dashboard sharing.
- Public dashboards.
- Workspace ownership.
- Custom BI expressions.
- Export or print output.
