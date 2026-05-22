# Task V1: Basic Field Builder

## Goal

Allow builders to add/edit/delete V1 field types and field settings in a draft form.

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

Completed for the current V1 slice. Builders can open `/forms/:formId/builder`, add/edit/delete V1 fields, edit basic settings, and save draft schemas through the backend draft endpoint.

## Out of Scope

Do not implement advanced validation or conditional visibility.

## Notes

Implement only this task. Do not add unrelated features.
