# V8 Task 005: Record Import Jobs

## Goal

Add a controlled record import job foundation for permitted form records.

## Context

Read:

- `docs/V8_START_HERE.md`
- `docs/API_SPEC.md`
- `docs/DATA_MODEL.md`
- `tasks/v8/001-api-keys-and-integration-auth.md`
- `tasks/v8/002-integration-logs-and-retry-foundation.md`
- `tasks/v8/003-public-record-api-foundation.md`
- `AGENTS.md`

## Requirements

- Support CSV import first.
- Validate headers and field mappings before creating records.
- Use existing record creation validation and permissions.
- Track import job status, row counts, failed rows, and sanitized errors.
- Do not create partial records without clear row-level results.
- Add audit/integration logs.

## Acceptance Criteria

- [x] Import job contracts are typed.
- [x] CSV import supports explicit field mapping.
- [x] Record validation is reused.
- [x] Failed rows are reported safely.
- [x] Import status is persisted and queryable.
- [x] Documentation is updated.
- [x] Tests are added where practical.
- [x] Relevant build/test commands are run.

## Implementation Notes

- Added `record_import_jobs` and `record_import_job_rows` through EF Core migration `20260610133807_RecordImportJobs`.
- Added typed import job contracts for statuses, row statuses, explicit CSV-header-to-target-field mappings, job summaries/details, row results, and validation results.
- Added a dependency-light CSV parser for the first slice, including quoted value support.
- Management/query endpoints live under `/api/integrations/imports` and require cookie auth plus `integrations.manage`; the service also checks existing form submit permission before creating records.
- Job creation validates CSV headers and target field mappings before processing rows.
- Each row is submitted through the existing `RecordSubmissionService`, preserving form versioning, record validation, audit logs, and trigger dispatch.
- Row-level failures persist sanitized validation errors without storing raw CSV payloads or hidden field values.
- Completed imports write inbound `import` integration logs with row counts and operational metadata.

## Out of Scope

- Excel parsing unless explicitly justified.
- External database sync.
- Arbitrary transforms.
- Cross-form import.
