# Task V2: Form Data Readiness

## Goal

Prepare published form schemas and submitted record data for reliable report and dashboard generation.

## Context

Read:

- `docs/MASTER_PRD_FOR_AI.md`
- `docs/REPORTS_AND_PRINTING.md`
- `docs/superpowers/specs/2026-05-29-v2-form-data-reports-dashboards-design.md`
- `AGENTS.md`

## Acceptance Criteria

- [x] Reportable form field metadata is available through shared frontend/backend helpers.
- [x] Field labels, types, option labels, and system fields are normalized for report/dashboard use.
- [x] Existing published form versions remain immutable.
- [x] Existing V1 form submission and record flows continue to pass.
- [x] Tests are added where practical.
- [x] Documentation is updated if contracts change.

## Current Status

Completed for the current V2 slice. Frontend and backend now expose shared reportable field metadata helpers for form fields, choice options, and normalized system fields. Existing report definition building and backend report config validation consume that metadata. This prepares report execution, dashboard summaries, chart widgets, validation rules, triggers, workflows, scheduled triggers, and future actions without changing persistence or published form versions.

## Out of Scope

Do not implement dashboards, charts, CSV export, PDF templates, triggers, or workflow in this task.
