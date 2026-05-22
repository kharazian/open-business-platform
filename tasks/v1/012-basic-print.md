# Task V1: Basic Print

## Goal

Add browser print support for record detail and current record list.

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

Completed for the current V1 slice. The record list and record detail pages expose browser print actions, use scoped print-only headings, hide app chrome and interactive controls while printing, and apply basic print CSS for tables and record value sections.

## Out of Scope

Do not implement PDF generation.

## Notes

Implement only this task. Do not add unrelated features.
