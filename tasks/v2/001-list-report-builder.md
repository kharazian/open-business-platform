# Task V2: List Report Builder

## Goal

Create report definitions for list reports with columns, filters, and sorting.

## Context

Read:

- `docs/MASTER_PRD_FOR_AI.md`
- `docs/REPORTS_AND_PRINTING.md`
- `AGENTS.md`

## Acceptance Criteria

- [x] Feature works according to V2 roadmap.
- [x] Backend permission checks are included.
- [x] Tests are added where practical.
- [x] Documentation is updated if contracts change.

## Current Status

Completed for the current V2 slice. Users with report management and form manage access can save list report definitions with selected, reordered, and custom-labeled columns, multiple saved UI filters, and multiple saved UI sorts. The builder shows inline validation for active filters that require values plus invalid or duplicate sort fields before save. Saved filters now expose type-aware operators, inputs, and value validation for text, choice, numeric, date, datetime, and time fields. The backend persists report config JSONB, validates config against the form schema plus supported system fields, checks permissions, executes type-aware saved filters, and writes report audit entries.

## Out of Scope

Do not implement dashboards, PDF templates, or workflow.
