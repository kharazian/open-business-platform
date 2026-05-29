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

- [ ] Supported widgets include number card, bar chart, date trend, status/choice breakdown, and table.
- [ ] Chart configs validate selected fields, metrics, and groupings against the source form/report.
- [ ] Backend aggregation endpoints enforce permissions.
- [ ] Frontend renders chart widgets without adding a large chart dependency unless justified.
- [ ] Tests are added where practical.
- [ ] Documentation is updated if contracts change.

## Out of Scope

Do not implement XYFlow, workflow diagrams, advanced BI expressions, PDF templates, or cross-form joins in this task.
