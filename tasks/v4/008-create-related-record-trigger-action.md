# V4 Task 008: Create Related Record Trigger Action

## Goal

Add a typed trigger action that can create a related record in another published form after a record event succeeds.

## Context

Read:

- `docs/MASTER_PRD_FOR_AI.md`
- `docs/ROADMAP.md`
- `docs/TRIGGERS_AND_WORKFLOWS.md`
- `docs/API_SPEC.md`
- `docs/DATA_MODEL.md`
- `tasks/v4/001-trigger-engine-foundation.md`
- `tasks/v4/003-update-field-trigger-action.md`
- `tasks/v4/004-trigger-retry-recovery.md`
- `tasks/v4/005-in-app-notification-trigger-action.md`
- `AGENTS.md`

## Requirements

- Add `create_record` as a typed, approved trigger action.
- Require a target form ID and a field-value map for the target record.
- Create target records only against the latest published target form version.
- Validate target form existence, published version existence, target field IDs, required fields, and record values when saving trigger definitions.
- Support literal values and safe source-record field references as value inputs.
- Store a relationship from the created record back to the source record in trigger/action metadata or another existing extensibility field.
- Execute after the source record transaction succeeds, following the existing non-recursive trigger execution rules.
- Write trigger log result details with the created record ID.
- Add `create_record` editing support to the trigger management UI.
- Keep formulas, custom code, target-form draft creation, recursive trigger chaining, bulk creates, and workflow starts out of scope.

## Acceptance Criteria

- [x] Backend validation accepts a valid `create_record` trigger action.
- [x] Backend validation rejects missing target form ID, unpublished target forms, unknown fields, invalid values, and missing required target field values.
- [x] Backend execution creates a target record using the latest published target form version.
- [x] Backend execution preserves source trigger/log metadata on the created record or related metadata payload.
- [x] Backend execution does not dispatch recursive triggers from the created target record in this slice.
- [x] Trigger logs include the created record ID on success and actionable validation errors on failure.
- [x] Frontend trigger builder can create and edit `create_record` actions.
- [x] Existing trigger actions continue to work.
- [x] Documentation is updated if contracts, schemas, or trigger behavior change.
- [x] Relevant tests/builds are run.

## Current Status

Completed for the current V4 action slice. `create_record` is available as a typed trigger action with backend target-form validation, latest published form-version creation, source/literal value mappings, source trigger metadata on created records, trigger log result details, and frontend builder support.

## Out of Scope

- Update related record actions.
- Multi-record creation.
- Formula/expression language support.
- Trigger chaining from records created by trigger actions.
- Workflow starts or approval routing.
- Webhook actions.
- Scheduled triggers.
- Automatic retry queues.

## Tests

- Add backend harness coverage for `create_record` action validation and execution contracts.
- Add backend harness coverage that created records use the target form's latest published version.
- Add backend harness coverage that invalid target field/value inputs are rejected.
- Add frontend trigger builder tests for `create_record` payload creation and validation.
- Run `dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj`.
- Run `dotnet build src/api/OpenBusinessPlatform.Api.csproj`.
- Run `npm test` and `npm run build` in `src/app`.

## Notes

- Prefer reusing the existing record submission/value validation service path instead of duplicating target record validation.
- Keep the action small and auditable. It should create one record and report that result clearly in trigger logs.
