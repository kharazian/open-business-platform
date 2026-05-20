# Task V1: Form List and Create

## Goal

Add basic API and UI to list forms and create a form draft.

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

Completed for the current skeleton. `GET /api/forms` and `POST /api/forms` persist draft forms through the Forms backend module, and the `/forms` frontend page lists, filters, refreshes, and creates forms through `src/app/src/features/forms/api.ts`.

## Out of Scope

Do not implement full field builder yet.

## Notes

Implement only this task. Do not add unrelated features.
