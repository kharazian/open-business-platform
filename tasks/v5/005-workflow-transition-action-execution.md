# V5 Task 005: Workflow Transition Action Execution

## Goal

Execute typed workflow transition actions after a direct transition or approval-completed transition, while preserving workflow version references, record consistency, auditability, and deterministic failure behavior.

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
- `tasks/v5/003-record-workflow-transition-execution.md`
- `tasks/v5/004-approval-inbox-and-notifications.md`
- `tasks/v4/001-trigger-engine-foundation.md`
- `tasks/v4/008-create-related-record-trigger-action.md`
- `tasks/v4/010-webhooks-retry-policies-scheduled-triggers.md`
- `AGENTS.md`

## Requirements

- Add a backend workflow action execution service that executes transition `actions` from the exact published workflow definition version stored on the record.
- Invoke action execution after direct transitions and after approval completion moves the record.
- Execute actions in configured order and preserve the action id/type in logs, history, audit metadata, and error output.
- Reuse existing trigger action primitives where safe instead of creating a separate workflow-only action stack.
- Support the safe V5 action subset first: `write_audit_entry`, `send_email`, `send_notification`, `assign_record`, `update_field`, and `create_record`.
- Treat `change_status` carefully so workflow state and `records.status` cannot diverge. If it cannot be implemented safely in this slice, reject it during workflow validation or leave it documented as a placeholder until a later task.
- Keep `call_webhook`, generated PDFs, external integrations, custom code, and workflow action retry queues out of this task unless they are required for compatibility with an existing supported action.
- Add workflow action execution persistence or workflow-history metadata sufficient to audit each attempted action, status, error message, start time, and completion time.
- Define deterministic failure behavior:
  - record-mutating action failures must not leave a partially completed workflow transition;
  - notification/email failures must be logged clearly and must not hide the transition outcome from the user;
  - failed actions must not recursively dispatch triggers unless explicitly allowed by the action semantics.
- Ensure approval-completed transitions and direct transitions share the same action execution path.
- Preserve existing status-changed trigger dispatch semantics after the workflow transition commits.
- Add frontend workflow builder helpers/UI affordances only as needed to edit action configs more safely than raw JSON.
- Update docs for any new tables, history actions, API behavior, and supported action types.

## Acceptance Criteria

- [x] Backend executes supported transition actions for direct transitions.
- [x] Backend executes supported transition actions when an approval completes a transition.
- [x] Actions run against the workflow definition version stored on the record, not a mutable draft.
- [x] Action execution is logged or represented in workflow history with action id, type, status, error, started time, and completed time.
- [x] Failed record-mutating actions do not leave partially applied workflow state or record values.
- [x] Workflow action execution does not introduce recursive trigger/workflow loops.
- [x] Unsupported or unsafe action types are rejected or clearly skipped according to documented behavior.
- [x] Frontend workflow action editing is improved where practical without adding a visual graph builder.
- [x] Documentation is updated for supported workflow action behavior and persistence.
- [x] Relevant tests/builds are run.

## Current Status

Completed for the current V5 action execution slice. Direct transitions and approval-completed transitions now execute supported transition actions from the stored published workflow version. Action attempts are represented in `workflow_history` as `workflow_action_succeeded` or `workflow_action_failed` rows with action id/type/status/error/start/completion metadata. Record-mutating action failures roll back the workflow transition and persist rollback failure history afterward; email and notification failures are logged without hiding the transition outcome. `change_status`, `call_webhook`, generated PDFs, custom code, and workflow action retry queues remain out of scope.

## Out of Scope

- Trigger-to-workflow starts.
- Scheduled workflow starts.
- Incoming webhook listeners.
- Workflow action retry queues beyond clear failure logging.
- `call_webhook` execution unless explicitly added as a safe V5 action in this task.
- PDF generation or print-template actions.
- XYFlow or visual workflow diagrams.
- Custom code execution.

## Tests

- Add backend harness coverage for action execution contracts and persistence.
- Add backend harness coverage for direct-transition action execution.
- Add backend harness coverage for approval-completed action execution.
- Add backend harness coverage for rollback/failure behavior where practical.
- Add frontend tests for any workflow action editor/helper changes.
- Run `dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj`.
- Run `dotnet build src/api/OpenBusinessPlatform.Api.csproj`.
- Run `npm test` and `npm run build` in `src/app` when frontend code changes.

## Notes

- The implementation should make action execution reusable by later trigger-to-workflow and scheduled-workflow tasks.
- Keep the first slice conservative. It is better to execute fewer typed actions safely than to allow broad action semantics that can desynchronize records, workflows, reports, and triggers.
