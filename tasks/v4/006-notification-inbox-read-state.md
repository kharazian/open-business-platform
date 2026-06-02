# V4 Task 006: Notification Inbox and Read State

## Goal

Add current-user notification inbox APIs and a real notifications page.

## Context

Read:

- `docs/MASTER_PRD_FOR_AI.md`
- `docs/ROADMAP.md`
- `docs/TRIGGERS_AND_WORKFLOWS.md`
- `docs/API_SPEC.md`
- `docs/DATA_MODEL.md`
- `tasks/v4/005-in-app-notification-trigger-action.md`
- `AGENTS.md`

## Requirements

- Add authenticated APIs for the current user's notifications.
- List newest notifications first.
- Return unread count for the current user.
- Mark one notification as read.
- Mark all current-user notifications as read.
- Do not allow users to read or mutate another user's notifications.
- Add a real `/notifications` frontend page.
- Keep push delivery, websockets, email fallback, and admin notification management out of scope.

## Acceptance Criteria

- [x] Backend exposes `GET /api/notifications`.
- [x] Backend exposes `GET /api/notifications/unread-count`.
- [x] Backend exposes `POST /api/notifications/{notificationId}/read`.
- [x] Backend exposes `POST /api/notifications/read-all`.
- [x] Backend APIs use the authenticated current user id.
- [x] Frontend notification API client covers list, unread count, mark read, and mark all read.
- [x] Frontend `/notifications` page lists notifications and read state.
- [x] Frontend page can mark one notification or all notifications read.
- [x] Documentation is updated.
- [x] Relevant tests/builds are run.

## Current Status

Completed for the current V4 notification inbox slice. The backend exposes authenticated current-user inbox/read-state APIs, and the frontend registers a real `/notifications` page with list, unread count, mark-one-read, and mark-all-read workflows.

## Out of Scope

- Push notifications.
- Websockets.
- Header unread badge.
- Notification preferences.
- Notification templates.
- Admin notification management.
- Cross-user notification browsing.

## Tests

- Add backend harness checks for notification service/module contracts.
- Add frontend API tests for notification endpoints.
- Add frontend module/page routing tests where practical.
- Run `dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj`.
- Run `dotnet build src/api/OpenBusinessPlatform.Api.csproj`.
- Run `npm test` and `npm run build` in `src/app`.
