# Task V1: Record Edit and Delete

## Goal

Allow authorized users to edit and soft-delete records.

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

Completed for the current V1 slice. Authorized users can edit record values against the record's stored immutable form version schema and soft-delete records through backend-checked endpoints and frontend detail actions.

## Out of Scope

Do not implement workflow status locking yet.

## Notes

Implement only this task. Do not add unrelated features.
