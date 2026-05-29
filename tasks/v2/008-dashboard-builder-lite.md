# Task V2: Dashboard Builder Lite

## Goal

Allow users to save a simple dashboard layout made of permitted chart/report widgets.

## Context

Read:

- `docs/MASTER_PRD_FOR_AI.md`
- `docs/ROADMAP.md`
- `docs/superpowers/specs/2026-05-29-v2-form-data-reports-dashboards-design.md`
- `AGENTS.md`

## Acceptance Criteria

- [ ] Dashboard definitions can be saved with widget config and layout JSON.
- [ ] Backend validation rejects unknown source forms/reports, fields, metrics, and widget types.
- [ ] Backend authorization is enforced.
- [ ] Frontend can create, view, and update a dashboard layout.
- [ ] Database migration is documented if a dashboards table is added.
- [ ] Tests are added where practical.
- [ ] Documentation is updated if contracts change.

## Out of Scope

Do not implement XYFlow, triggers, workflows, PDF templates, advanced permissions, or public sharing in this task.
