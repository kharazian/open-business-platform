# Task V1: Record Submission

## Goal

Allow users to submit a published form and store records in PostgreSQL with form version reference.

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

Completed for the current V1 slice. The backend exposes a submit-safe published form schema endpoint, record submission validates values against the current published schema and stores `form_version_id`, and the frontend includes an authenticated `/forms/:formId/submit` page using the shared form renderer.

## Out of Scope

Do not implement triggers yet.

## Notes

Implement only this task. Do not add unrelated features.
