# V4 Task 007: Notification Badges and Preferences

## Goal

Finish the V4 notification slice with unread navigation badges and current-user notification preferences.

## Context

Read:

- `docs/MASTER_PRD_FOR_AI.md`
- `docs/ROADMAP.md`
- `docs/TRIGGERS_AND_WORKFLOWS.md`
- `docs/API_SPEC.md`
- `docs/DATA_MODEL.md`
- `tasks/v4/005-in-app-notification-trigger-action.md`
- `tasks/v4/006-notification-inbox-read-state.md`
- `AGENTS.md`

## Requirements

- Add persisted current-user notification preferences.
- Add authenticated APIs to read and update the current user's preferences.
- Default safely for users without a stored preference row.
- Respect disabled in-app notifications when trigger actions create notifications.
- Add unread notification badges to app navigation when enabled.
- Add a preferences panel to the real `/notifications` page.
- Keep push delivery, websockets, email fallback, admin notification management, automatic retry queues, and webhook actions out of scope.

## Acceptance Criteria

- [x] Backend exposes `GET /api/notifications/preferences`.
- [x] Backend exposes `PUT /api/notifications/preferences`.
- [x] Preferences persist in PostgreSQL and default safely for users without a row.
- [x] Trigger-created in-app notifications skip users who disabled in-app notifications.
- [x] App navigation shows unread badges when enabled.
- [x] `/notifications` lets users update in-app and badge preferences.
- [x] Documentation is updated.
- [x] Relevant tests/builds are run.

## Current Status

Completed for the current V4 notification closure slice. The backend stores current-user notification preferences, trigger notification actions respect disabled in-app delivery, and the frontend shows preference-controlled unread badges plus a preferences panel on `/notifications`.

## Out of Scope

- Push notifications.
- Websockets or server-sent event updates.
- Email fallback or digest delivery.
- Notification templates.
- Admin notification management.
- Cross-user notification browsing.
- Automatic trigger retry queues.
- Webhook actions.
- Scheduled triggers.

## Tests

- Add backend harness checks for notification preference entity, indexes, DTOs, and service contracts.
- Add frontend API tests for preference read/update endpoints.
- Add frontend helper tests for notification badge formatting and navigation enrichment.
- Run `dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj`.
- Run `dotnet build src/api/OpenBusinessPlatform.Api.csproj`.
- Run `npm test` and `npm run build` in `src/app`.
