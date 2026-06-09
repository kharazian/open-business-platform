# V7 Task 004: Dashboard Sharing And Defaults

## Goal

Define and implement conservative saved dashboard defaults and visibility rules after the V7 analytics builder/viewer foundation exists.

## Context

Read:

- `docs/MASTER_PRD_FOR_AI.md`
- `docs/ROADMAP.md`
- `docs/API_SPEC.md`
- `docs/DATA_MODEL.md`
- `docs/V7_START_HERE.md`
- `tasks/v7/001-advanced-dashboard-analytics-foundation.md`
- `tasks/v7/002-dashboard-widget-builder-upgrade.md`
- `tasks/v7/003-dashboard-viewer-refresh.md`
- `AGENTS.md`

## Requirements

- Keep dashboard visibility backend-enforced.
- Preserve existing dashboard management permissions.
- Add only the minimum defaults/visibility needed before the workspace module exists.
- Do not build cross-workspace sharing.
- Document any new ownership, default, or visibility fields before adding a migration.
- Keep reports and dashboards separate.

## Acceptance Criteria

- [ ] Dashboard defaults are explicit and backend-owned.
- [ ] Visibility rules are enforced by the API.
- [ ] Existing dashboards remain accessible under the old permission model or are migrated safely.
- [ ] Any schema change has a documented migration.
- [ ] Tests are added where practical.
- [ ] Documentation is updated.
- [ ] Relevant build/test commands are run.

## Out of Scope

- Full workspace ownership.
- Public links.
- External sharing.
- Tenant-level dashboard policies.
- Dashboard export or print.
