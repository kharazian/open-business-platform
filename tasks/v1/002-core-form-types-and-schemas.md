# Task V1: Core Form Types and Schemas

## Goal

Create shared frontend/backend representations for form fields, layout, form version, validation, and record values.

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

Completed for the V1 baseline. Shared V1 form field, layout, form version, validation, and record value contracts exist in `src/app/src/features/forms` and `src/api/Modules/Forms`. `npm test`, `npm run build`, and `dotnet build` passed.

## Out of Scope

No UI beyond what is needed for type/schema tests.

## Notes

Implement only this task. Do not add unrelated features.
