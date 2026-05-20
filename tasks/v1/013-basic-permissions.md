# Task V1: Basic Permissions

## Goal

Implement V1 roles and backend permission checks for submit/view/edit/delete.

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

- [ ] The task goal is implemented.
- [ ] Existing behavior is not broken.
- [ ] Backend authorization is respected where applicable.
- [ ] Build/test commands are run if available.
- [ ] Files changed and risks are summarized.

## Current Status

Partially complete. The current code has local users, roles, menu visibility, per-form role access rows, effective permissions, and backend checks for auth, users, roles, dashboard, and forms list/create. Record submit/view/edit/delete permission checks remain pending with the record engine tasks.

## Out of Scope

Do not implement advanced rule builder.

## Notes

Implement only this task. Do not add unrelated features.
