# Task V1: Form Renderer and Preview

## Goal

Render forms from schema/layout and add preview mode.

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

Completed for the current local form builder. A reusable V1 `FormRenderer` now renders schema layout and field controls, and the builder opens a local preview modal with mobile/tablet/desktop sizing and record-value validation. Preview uses local draft state only and does not submit records.

## Out of Scope

- Persisting preview submissions as records.
- Publishing form versions.
- Backend draft schema persistence.

Preview should reuse the same renderer as submitted forms.

## Notes

Implement only this task. Do not add unrelated features.
