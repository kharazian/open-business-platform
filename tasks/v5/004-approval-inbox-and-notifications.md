# V5 Task 004: Approval Inbox and Notifications

## Goal

Add a current-user approval inbox for workflow approval-gated transitions, with in-app notifications for assigned approvers.

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
- `AGENTS.md`

## Requirements

- Add workflow approval task persistence for approval-gated transitions.
- Resolve approval assignee rules for active users, active group members, active department managers, and record owner.
- When a user requests an approval-gated transition, create one pending approval task per resolved approver and notify each approver who has in-app notifications enabled.
- Do not create duplicate pending approval groups for the same record, workflow version, transition, and from-state.
- Add current-user approval inbox APIs to list pending/recent approvals, approve one assigned task, and reject one assigned task.
- Support approval mode `any`: one approval executes the transition and cancels sibling pending tasks.
- Support approval mode `all`: execute the transition only after every assigned approver has approved.
- Rejection cancels sibling pending tasks and leaves the record in the current state.
- Preserve record workflow version references and write workflow history/audit entries for approval requests, approvals, rejections, cancellations, and completed transitions.
- Dispatch the existing status-changed trigger event when approval completion moves the record.
- Add a real frontend approval inbox route/page with approve/reject actions and record links.
- Keep workflow transition action execution, email fallback, push/websocket delivery, scheduled starts, trigger-to-workflow starts, and XYFlow out of scope.

## Acceptance Criteria

- [x] Database migration adds workflow approval task persistence.
- [x] Backend creates approval tasks and notifications when a transition requires approval.
- [x] Backend current-user approval APIs list, approve, and reject assigned tasks.
- [x] `any` and `all` approval modes move records only when the mode is satisfied.
- [x] Rejections cancel pending sibling tasks and do not move the record.
- [x] Workflow history and audit logs capture approval request/response activity.
- [x] Frontend has typed approval API helpers and an approval inbox page.
- [x] Documentation is updated for new tables and APIs.
- [x] Relevant tests/builds are run.

## Current Status

Implemented for the current approval execution slice.

## Out of Scope

- Executing workflow transition action placeholders.
- Email fallback, push notifications, or websocket delivery.
- Trigger-to-workflow starts.
- Scheduled workflow starts.
- XYFlow or visual workflow diagrams.
- Admin approval reassignment tools.

## Tests

- Add backend harness coverage for approval task entity/model mapping and approval contracts.
- Add backend harness coverage for approval mode/status helpers where practical.
- Add frontend helper tests for approval API endpoint shapes.
- Run `dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj`.
- Run `dotnet build src/api/OpenBusinessPlatform.Api.csproj`.
- Run `npm test` and `npm run build` in `src/app`.

## Notes

- Approval inbox tasks are per assigned user so the current-user inbox can stay simple and permission-safe.
- The first workflow notification implementation uses existing in-app notification rows and current-user notification preferences.
