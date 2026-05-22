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

Completed for the current V2 slice. Users with report management and form manage access can save list report definitions with selected columns, one UI filter, and one UI sort. The backend persists report config JSONB, validates config against the form schema plus supported system fields, checks permissions, and writes `report_created` audit entries.

## Out of Scope

Do not implement dashboards, PDF templates, or workflow.
