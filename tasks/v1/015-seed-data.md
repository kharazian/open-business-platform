# Task V1: Seed Data

## Goal

Add seed/demo data for users, roles, departments, sample form, and records.

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

Completed for the current V1 slice. In development, API startup runs an idempotent demo data seeder that creates demo users, roles, departments, a published Employee Information Form, per-form access rows, and 10 sample employee records when the database is available.

## Out of Scope

Do not seed advanced reports/triggers/workflows yet.

## Notes

Implement only this task. Do not add unrelated features.
