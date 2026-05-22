# Task V1: Publish Form Version

## Goal

Publish an immutable form version from the draft schema/layout.

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

Completed for the current V1 slice. The backend saves draft schemas, publishes immutable form versions from the saved draft, updates the current version pointer, enforces manage access, and records a `form_published` audit log entry.

## Out of Scope

Published versions must not be mutated.

## Notes

Implement only this task. Do not add unrelated features.
