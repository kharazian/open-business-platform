# V8 Task 006: External Export Jobs

## Goal

Add outbound export jobs for permitted record/report data with explicit logs and safe field filtering.

## Context

Read:

- `docs/V8_START_HERE.md`
- `docs/API_SPEC.md`
- `docs/DATA_MODEL.md`
- `tasks/v8/001-api-keys-and-integration-auth.md`
- `tasks/v8/002-integration-logs-and-retry-foundation.md`
- `tasks/v2/003-csv-export.md`
- `AGENTS.md`

## Requirements

- Export permitted list report or form record data through explicit jobs.
- Reuse report permissions, form permissions, record scopes, and hidden-field filtering.
- Support CSV or JSON output first.
- Track job status and generated artifact metadata.
- Add integration/audit logs for export actions.
- Avoid long-running synchronous exports when a job model is safer.

## Acceptance Criteria

- [x] Export job contracts are typed.
- [x] Exports respect report/form/field/record permissions.
- [x] Export output excludes hidden fields.
- [x] Job status is persisted and queryable.
- [x] Integration/audit logs are written.
- [x] Documentation is updated.
- [x] Tests are added where practical.
- [x] Relevant build/test commands are run.

## Implementation Notes

- Added `external_export_jobs` through EF Core migration `20260610143706_ExternalExportJobs`.
- Added typed export job contracts for source types, formats, statuses, artifact metadata, summaries, and details.
- Added protected `/api/integrations/exports` list/get/create endpoints requiring cookie auth plus `integrations.manage`.
- Form record exports require form `export` access and apply existing record-scope and hidden-field filtering.
- List report exports require report `export` access and reuse report export execution with form/report/record/hidden-field filtering.
- CSV and JSON artifacts are generated without public download links; job detail returns protected artifact content and metadata.
- Export jobs write audit entries and outbound `export` integration logs.

## Out of Scope

- Public download links.
- Dashboard exports.
- PDF exports unless a later task explicitly adds them.
- Arbitrary SQL exports.
