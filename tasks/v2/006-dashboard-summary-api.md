# Task V2: Dashboard Summary API

## Goal

Replace starter dashboard data with backend-owned real metrics from users, forms, records, reports, and audit logs.

## Context

Read:

- `docs/MASTER_PRD_FOR_AI.md`
- `docs/ROADMAP.md`
- `docs/superpowers/specs/2026-05-29-v2-form-data-reports-dashboards-design.md`
- `AGENTS.md`

## Acceptance Criteria

- [x] Dashboard summary endpoint returns real database-backed metrics.
- [x] Backend authorization is enforced.
- [x] Frontend dashboard uses the dashboard API instead of sample data.
- [x] Loading, empty, and error states are handled.
- [x] Tests are added where practical.
- [x] Documentation is updated if contracts change.

## Current Status

Completed for the current V2 slice. The dashboard summary endpoint now requires `menu.dashboard`, returns database-backed counts for users, forms, records, reports, and audit logs, and includes recent audit activity. The frontend dashboard uses this API with loading, retry/error, and empty activity states instead of starter sample dashboard data.

## Out of Scope

Do not implement a saved dashboard builder, triggers, workflows, PDF templates, or advanced permissions in this task.
