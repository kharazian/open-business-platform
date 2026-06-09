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

- [ ] Import job contracts are typed.
- [ ] CSV import supports explicit field mapping.
- [ ] Record validation is reused.
- [ ] Failed rows are reported safely.
- [ ] Import status is persisted and queryable.
- [ ] Documentation is updated.
- [ ] Tests are added where practical.
- [ ] Relevant build/test commands are run.

## Out of Scope

- Excel parsing unless explicitly justified.
- External database sync.
- Arbitrary transforms.
- Cross-form import.
