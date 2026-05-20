# Task V1: Database Foundation

## Goal

Add database entities/tables for forms, form versions, records, users/roles if needed, and audit logs.

## Context

Read before starting:

- `docs/MASTER_PRD_FOR_AI.md`
- `AGENTS.md`
- Relevant docs for this task

## Requirements

- Follow the existing project conventions.
- Keep frontend and backend responsibilities separated.
- Add or update tests where practical.
- Update documentation if commands, architecture, or contracts change.

## Acceptance Criteria

- [x] The task goal is implemented.
- [x] Existing behavior is not broken.
- [x] Backend authorization is respected where applicable.
- [x] Build/test commands are run if available.
- [x] Files changed and risks are summarized.

## Current Status

Completed for the current skeleton. EF Core/Npgsql entities, mappings, migrations, indexes, JSONB columns, Guid IDs, audited entity bases, and lightweight backend harness checks exist for users, roles, departments, forms, form versions, records, role permissions, form permissions, and audit logs.

## Out of Scope

Do not implement reports, triggers, or workflows.

## Notes

Implement only this task. Do not add unrelated features.
