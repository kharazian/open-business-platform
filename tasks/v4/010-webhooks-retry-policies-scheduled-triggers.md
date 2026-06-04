# V4 Task 010: Webhooks, Retry Policies, and Scheduled Triggers

## Goal

Close the remaining V4 automation surface with typed webhook actions, user-authored retry policy metadata, and a conservative scheduled trigger runner.

## Requirements

- Add `call_webhook` as an approved trigger action.
- Add editable automatic retry policy settings per trigger.
- Add schedule event names and schedule metadata for once, daily, weekly, and monthly schedules.
- Add a hosted schedule worker that runs due enabled scheduled triggers.
- Keep scheduled triggers limited to safe non-record actions in V4.
- Keep webhook listeners, custom code execution, workflow approvals, and XYFlow out of scope.

## Acceptance Criteria

- [x] Trigger contracts include webhook action fields, retry policy metadata, and schedule metadata.
- [x] Backend validation rejects invalid webhook URLs/methods, out-of-range retry policies, missing schedule metadata, and record-dependent scheduled actions.
- [x] Trigger definitions persist retry policy and schedule metadata.
- [x] Failed trigger logs use the trigger's configured retry policy.
- [x] Due scheduled triggers execute through a hosted worker and write normal trigger logs.
- [x] The trigger management UI can edit webhook actions, retry settings, and schedule fields.
- [x] Documentation and V4 task index are updated.
- [x] Tests/builds are run.

## Out of Scope

- Incoming webhook listeners.
- Custom JavaScript or custom code actions.
- Record/workflow transition execution from schedules.
- Visual automation diagrams.

## Current Status

Completed for the V4 automation closure slice.
