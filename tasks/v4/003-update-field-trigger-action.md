# V4 Task 003: Update Field Trigger Action

## Goal

Add a safe trigger action that updates one field on the current record.

## Context

Read:

- `docs/MASTER_PRD_FOR_AI.md`
- `docs/ROADMAP.md`
- `docs/TRIGGERS_AND_WORKFLOWS.md`
- `docs/API_SPEC.md`
- `tasks/v4/001-trigger-engine-foundation.md`
- `tasks/v4/002-trigger-management-ui.md`
- `AGENTS.md`

## Requirements

- Add `update_field` as a typed, approved trigger action.
- Require a valid form field reference and a value payload.
- Validate the resulting record values against the form schema before saving.
- Write a record audit entry with before/after values and trigger metadata.
- Suppress recursive trigger dispatch, matching current status and assignment trigger actions.
- Add `update_field` to the trigger management UI.

## Acceptance Criteria

- [x] Backend trigger definition validation accepts valid `update_field` actions.
- [x] Backend trigger definition validation rejects missing/unknown field references and missing values.
- [x] Backend trigger execution updates the current record values JSON.
- [x] Backend trigger execution validates the updated values against the record form version schema.
- [x] Backend trigger execution writes a record audit entry for the field update.
- [x] Frontend trigger builder can create and edit `update_field` actions.
- [x] Existing behavior is not broken.
- [x] Documentation is updated if contracts or trigger behavior change.
- [x] Relevant tests/builds are run.

## Current Status

Completed for the current V4 slice. `update_field` is available as a typed trigger action with backend validation, non-recursive current-record execution, record audit logging, and frontend trigger builder support.

## Out of Scope

- Create related records.
- Bulk field updates.
- Cross-record updates.
- Custom code or expression evaluation.
- Recursive trigger dispatch.
- Retry queue/background worker.
- Scheduled triggers.
- Webhooks.
- Workflows or approvals.

## Tests

- Add backend harness tests for `update_field` validation.
- Add frontend trigger builder tests for `update_field` payload creation and validation.
- Run `dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj`.
- Run `dotnet build src/api/OpenBusinessPlatform.Api.csproj`.
- Run `npm test` and `npm run build` in `src/app`.

## Notes

- Implement only this task.
- Keep field values as literal JSON-compatible values. No expressions in this slice.
