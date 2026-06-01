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

- [x] Dashboard definitions can be saved with widget config and layout JSON.
- [x] Backend validation rejects unknown source forms/reports, fields, metrics, and widget types.
- [x] Backend authorization is enforced.
- [x] Frontend can create, view, and update a dashboard layout.
- [x] Database migration is documented if a dashboards table is added.
- [x] Tests are added where practical.
- [x] Documentation is updated if contracts change.

## Current Status

Completed for the current V2 slice. Saved dashboard definitions can be created, viewed, and updated separately from the system dashboard summary. Dashboard widget configs and layout JSON are persisted in PostgreSQL, validated on the backend, and rendered through the existing chart preview path. Workspace ownership is intentionally deferred to a later workspace module.

## Out of Scope

Do not implement XYFlow, triggers, workflows, PDF templates, advanced permissions, or public sharing in this task.
