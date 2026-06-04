# V5 Task 003: Record Workflow Transition Execution

## Goal

Let authorized users start a published workflow on a record and move that record through direct workflow transitions while preserving the exact workflow version and auditable history.

## Context

Read:

- `docs/MASTER_PRD_FOR_AI.md`
- `docs/ROADMAP.md`
- `docs/TRIGGERS_AND_WORKFLOWS.md`
- `docs/API_SPEC.md`
- `docs/DATA_MODEL.md`
- `docs/PERMISSIONS.md`
- `tasks/v5/001-workflow-engine-foundation.md`
- `tasks/v5/002-workflow-management-ui.md`
- `AGENTS.md`

## Requirements

- Add record-level workflow state that references the workflow definition and immutable published workflow version currently used by the record.
- Add backend APIs to get a record's workflow state, start an enabled published workflow, and execute a direct transition from the current state.
- Require existing record view access for reading workflow state.
- Require existing record `change_status` scoped access for starting or executing record workflow transitions.
- Validate record concurrency stamps on start and transition mutations.
- Reject starts when the workflow is disabled, unpublished, scoped to another form, or the record already has an active workflow.
- Reject transitions that do not exist, do not start from the record's current state, or require an approval step.
- Update the record status to the workflow state key on start and after a successful transition so existing record lists/reports reflect workflow progress.
- Write workflow history entries for start and transition events, including workflow definition/version IDs and actor metadata.
- Write record audit log entries for workflow start and transition mutations.
- Dispatch the existing status-changed trigger event when workflow execution changes record status.
- Add frontend helpers and record-detail controls for viewing workflow state, starting an available workflow, and executing available direct transitions.
- Keep approval inbox items, approval notifications, workflow action execution, trigger-to-workflow starts, scheduled workflow starts, XYFlow, and visual builders out of scope.

## Acceptance Criteria

- [x] Records persist `workflow_definition_id`, `workflow_definition_version_id`, and `workflow_state_key`.
- [x] Backend exposes authenticated, permission-protected record workflow state/start/transition endpoints.
- [x] Starting a workflow stores the published version, sets the initial state, updates record status, writes history, writes audit, and honors concurrency.
- [x] Executing a direct transition updates state/status, writes history, writes audit, honors concurrency, and rejects approval-gated transitions.
- [x] Record workflow state responses include available direct transitions and recent history.
- [x] Record detail UI can load workflow state, start an eligible workflow, and execute available direct transitions.
- [x] Documentation is updated for new record columns and APIs.
- [x] Relevant tests/builds are run.

## Current Status

Completed for the current V5 transition execution slice. Records now store active workflow definition/version/state, backend record workflow APIs support state reads, published workflow starts, and direct transitions, and record detail shows workflow state, start options, direct transition actions, and recent history.

## Out of Scope

- Approval inbox and delegated approval assignment resolution.
- Workflow notifications.
- Workflow transition action execution.
- Trigger-to-workflow starts.
- Scheduled workflow starts.
- XYFlow or other visual workflow diagrams.
- PDF generation or print template changes.
- Custom code execution.

## Tests

- Add backend harness coverage for record workflow state columns and indexes.
- Add backend harness coverage for record workflow DTO/service contracts.
- Add backend harness coverage for successful start/transition behavior and approval-gated transition rejection where practical.
- Add frontend helper tests for record workflow API endpoint shapes.
- Run `dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj`.
- Run `dotnet build src/api/OpenBusinessPlatform.Api.csproj`.
- Run `npm test` and `npm run build` in `src/app`.

## Notes

- This task intentionally uses direct transitions only. A transition with `approvalStepKey` proves that an approval is needed, but creating approval work items belongs to the next V5 approval-inbox slice.
- Updating `records.status` keeps existing record lists, reports, dashboards, and triggers aligned with workflow progress without introducing a second visible status field.
