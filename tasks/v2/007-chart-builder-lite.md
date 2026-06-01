# Task V2: Chart Builder Lite

## Goal

Create simple chart widget configuration and rendering over permitted form/report data.

## Context

Read:

- `docs/MASTER_PRD_FOR_AI.md`
- `docs/REPORTS_AND_PRINTING.md`
- `docs/superpowers/specs/2026-05-29-v2-form-data-reports-dashboards-design.md`
- `AGENTS.md`

## Acceptance Criteria

- [x] Supported widgets include number card, bar chart, date trend, status/choice breakdown, and table.
- [x] Chart configs validate selected fields, metrics, and groupings against the source form/report.
- [x] Backend aggregation endpoints enforce permissions.
- [x] Frontend renders chart widgets without adding a large chart dependency unless justified.
- [x] Tests are added where practical.
- [x] Documentation is updated if contracts change.

## Current Status

Completed for the current V2 slice. The backend exposes a permission-checked chart widget preview endpoint that validates widget configs against reportable form metadata, can use all form records or a saved report's filters as the source, and returns number, grouped, trend, or table preview data. The frontend adds a Charts V2 preview page with lightweight renderers and no charting dependency.

## Out of Scope

Do not implement XYFlow, workflow diagrams, advanced BI expressions, PDF templates, or cross-form joins in this task.
