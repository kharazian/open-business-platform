# V4 Task 005: In-App Notification Trigger Action

## Goal

Add a safe trigger action that creates in-app notifications for selected users or groups.

## Context

Read:

- `docs/MASTER_PRD_FOR_AI.md`
- `docs/ROADMAP.md`
- `docs/TRIGGERS_AND_WORKFLOWS.md`
- `docs/API_SPEC.md`
- `docs/DATA_MODEL.md`
- `tasks/v4/001-trigger-engine-foundation.md`
- `tasks/v4/002-trigger-management-ui.md`
- `tasks/v4/003-update-field-trigger-action.md`
- `tasks/v4/004-trigger-retry-recovery.md`
- `AGENTS.md`

## Requirements

- Add `send_notification` as a typed, approved trigger action.
- Persist in-app notification rows for selected active users and active group members.
- Require a notification title, body, and at least one user or group recipient.
- Validate referenced users and groups when saving trigger definitions.
- Include trigger/action/source metadata on persisted notifications.
- Add `send_notification` to the trigger management UI.
- Keep notification center pages, unread badges, push delivery, and websockets out of scope.

## Acceptance Criteria

- [x] Backend model includes a `notifications` table.
- [x] Backend validation accepts valid `send_notification` actions.
- [x] Backend validation rejects missing title, missing body, missing recipients, inactive/missing users, and inactive/missing groups.
- [x] Backend trigger execution creates one notification per resolved active user, deduplicating direct and group recipients.
- [x] Backend trigger execution writes notification metadata with trigger/action/log/source record context.
- [x] Frontend trigger builder can create and edit `send_notification` actions.
- [x] Existing behavior is not broken.
- [x] Documentation and migration notes are updated.
- [x] Relevant tests/builds are run.

## Current Status

Completed for the current V4 action slice. `send_notification` is available as a typed trigger action with backend validation, database-backed notification persistence, recipient deduplication, trigger metadata, frontend builder support, and EF migration `20260602172402_InAppNotifications`.

## Out of Scope

- Notification inbox UI.
- Notification read/unread APIs.
- Push notifications.
- Websockets.
- Email fallback.
- Notification templates.
- Cross-workspace notification routing.

## Tests

- Add backend harness tests for model mapping and `send_notification` validation.
- Add frontend trigger builder tests for `send_notification` payload creation and validation.
- Run `dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj`.
- Run `dotnet build src/api/OpenBusinessPlatform.Api.csproj`.
- Run `npm test` and `npm run build` in `src/app`.
