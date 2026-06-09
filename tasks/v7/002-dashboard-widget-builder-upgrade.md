# V7 Task 002: Dashboard Widget Builder Upgrade

## Goal

Upgrade the dashboard builder UI so users can configure widgets over the V7 dashboard analytics contract created in task 001.

## Context

Read:

- `docs/MASTER_PRD_FOR_AI.md`
- `docs/ROADMAP.md`
- `docs/REPORTS_AND_PRINTING.md`
- `docs/API_SPEC.md`
- `docs/V7_START_HERE.md`
- `tasks/v7/001-advanced-dashboard-analytics-foundation.md`
- `docs/superpowers/specs/2026-06-08-v7-advanced-dashboard-analytics-design.md`
- `AGENTS.md`

## Requirements

- Keep saved dashboards separate from reports and print templates.
- Use the V7 dashboard analytics API for widget previews where appropriate.
- Preserve existing saved dashboard definitions and V2 chart widget behavior.
- Add builder controls for summary, breakdown, trend, and table widgets.
- Support one source form and optional saved list report per widget.
- Keep validation and empty/error states clear without adding a large chart dependency.
- Keep the UI operational and dense enough for repeated admin use.

## Acceptance Criteria

- [ ] Users can configure V7 summary, breakdown, trend, and table widgets from the dashboard builder.
- [ ] Widget previews use the V7 analytics API.
- [ ] Existing V2 saved dashboards still load and render.
- [ ] Builder controls prevent obvious invalid combinations before submit.
- [ ] Backend validation remains the source of truth for permissions and hidden fields.
- [ ] Tests are added where practical.
- [ ] Documentation is updated if contracts or saved config change.
- [ ] Relevant build/test commands are run.

## Out of Scope

- Drag-and-drop layout redesign.
- Workspace sharing.
- Cross-form widgets.
- Custom formulas.
- Dashboard exports.
- Dashboard PDF output.
