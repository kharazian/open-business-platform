# V5 Task 006: Trigger-to-Workflow Starts

## Goal

Let trigger actions start eligible published workflows on records, using the existing workflow runner without creating recursive automation loops.

## Context

Read:

- `docs/MASTER_PRD_FOR_AI.md`
- `docs/ROADMAP.md`
- `docs/TRIGGERS_AND_WORKFLOWS.md`
- `docs/API_SPEC.md`
- `docs/DATA_MODEL.md`
- `docs/PERMISSIONS.md`
- `tasks/v4/001-trigger-engine-foundation.md`
- `tasks/v4/008-create-related-record-trigger-action.md`
- `tasks/v4/009-automatic-trigger-retry-queue.md`
- `tasks/v4/010-webhooks-retry-policies-scheduled-triggers.md`
- `tasks/v5/001-workflow-engine-foundation.md`
- `tasks/v5/003-record-workflow-transition-execution.md`
- `tasks/v5/005-workflow-transition-action-execution.md`
- `AGENTS.md`

## Requirements

- Add a typed trigger action for starting a workflow on the current record.
- Validate that the selected workflow is enabled, published, scoped to the same form as the record, and has a current published version.
- Reject or skip start attempts when the record already has an active workflow.
- Preserve existing record permission and audit expectations for workflow starts.
- Write workflow history and record audit entries that clearly show the workflow was started by a trigger action.
- Add trigger log result metadata for workflow start success, skip, or failure.
- Prevent recursive automation loops. Starting a workflow from a trigger must not recursively re-enter the same trigger chain unless a later task adds explicit guarded recursion support.
- Decide and document whether workflow-start status changes dispatch `status.changed` trigger events in this path. The default should be conservative and loop-safe.
- Add frontend trigger action editor support for choosing a form-scoped workflow when configuring the action.
- Keep scheduled workflow starts out of scope unless they can be implemented through the same action without record-context ambiguity.

## Acceptance Criteria

- [ ] Trigger definitions support a typed `start_workflow` action.
- [ ] Backend validation rejects missing, disabled, unpublished, wrong-form, or draft workflow targets.
- [ ] Trigger execution can start a workflow on the current record and write workflow history.
- [ ] Trigger logs include workflow start result metadata.
- [ ] Existing retry behavior records failed workflow start actions without duplicating active workflows.
- [ ] Recursive trigger/workflow loops are prevented and documented.
- [ ] Frontend trigger builder can configure the workflow-start action.
- [ ] Documentation is updated for API contracts, data model notes, permissions, and automation behavior.
- [ ] Relevant tests/builds are run.

## Current Status

Not started. This should follow V5 task 005 so trigger-started workflows can reuse the same transition/action execution assumptions.

## Out of Scope

- Scheduled workflow starts without a current record context.
- Incoming webhook listeners.
- Visual workflow diagrams.
- Workflow action retry queues beyond existing trigger retry behavior.
- Starting workflows on arbitrary records unrelated to the trigger context.
- Custom code execution.

## Tests

- Add backend harness coverage for trigger action constants and validation.
- Add backend harness coverage for successful workflow starts from trigger action execution.
- Add backend harness coverage for duplicate active workflow skip/failure behavior.
- Add backend harness coverage for loop prevention where practical.
- Add frontend tests for trigger builder action config helpers.
- Run `dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj`.
- Run `dotnet build src/api/OpenBusinessPlatform.Api.csproj`.
- Run `npm test` and `npm run build` in `src/app`.

## Notes

- This task should not make triggers and workflows one module. Triggers start workflows through a typed action boundary; workflows remain the owner of workflow state and history.
- Keep scheduled workflow starts for a later integration/scheduler task unless this task explicitly defines a safe record-context model.
