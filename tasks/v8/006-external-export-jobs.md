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

- [ ] Export job contracts are typed.
- [ ] Exports respect report/form/field/record permissions.
- [ ] Export output excludes hidden fields.
- [ ] Job status is persisted and queryable.
- [ ] Integration/audit logs are written.
- [ ] Documentation is updated.
- [ ] Tests are added where practical.
- [ ] Relevant build/test commands are run.

## Out of Scope

- Public download links.
- Dashboard exports.
- PDF exports unless a later task explicitly adds them.
- Arbitrary SQL exports.
