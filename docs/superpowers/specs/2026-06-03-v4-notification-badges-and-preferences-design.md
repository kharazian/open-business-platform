# V4 Notification Badges and Preferences Design

## Purpose

Finish the current V4 notification slice by making unread notifications visible from the main app navigation and giving each signed-in user a small set of persisted notification preferences.

This builds on V4 task 005, which creates database-backed in-app notifications from triggers, and V4 task 006, which exposes the current-user notification inbox and read-state APIs.

## Scope

Add a final V4 notification task covering:

- A current-user notification preferences model persisted in PostgreSQL.
- Authenticated APIs to read and update the current user's preferences.
- A notification unread badge in the app navigation.
- A preferences panel on the real `/notifications` page.
- Frontend API/types/tests for unread badge and preference workflows.
- Backend harness coverage for the preference contract where practical.

## Out Of Scope

- Push delivery.
- Websocket or server-sent event live updates.
- Email fallback or digest delivery.
- Admin notification management.
- Cross-user notification browsing.
- Automatic trigger retry queues.
- Webhook trigger actions.
- V5 workflows or approval flows.

## Backend Design

Add a `notification_preferences` table with one row per user. The row stores:

- `user_id`
- `in_app_enabled`
- `show_unread_badge`
- `updated_at`

The defaults are enabled in-app notifications and enabled unread badges. If a signed-in user has no stored row, the backend returns those defaults without requiring a seed row. Updating preferences creates or updates the row for the current user only.

Add endpoints under the existing authenticated `/api/notifications` group:

- `GET /api/notifications/preferences`
- `PUT /api/notifications/preferences`

Both endpoints use the current user's `ClaimTypes.NameIdentifier`. Users cannot read or update another user's preferences. The update request accepts the two booleans above and returns the saved preferences DTO.

Preference enforcement for trigger-created notifications will be limited to `in_app_enabled`: when a trigger action resolves recipients, users with `in_app_enabled = false` will be skipped. Missing preference rows behave as enabled. `show_unread_badge` affects only the current user's frontend navigation badge.

## Frontend Design

Extend `src/app/src/features/notifications` with preference types and API helpers.

Update `/notifications` to:

- Load preferences alongside notifications and unread count.
- Show a compact preferences panel with toggles for in-app notifications and unread badge.
- Save preference changes through the new API.
- Keep existing mark-one-read and mark-all-read workflows.

Update the shared app navigation so the Notifications item can show an unread count badge when:

- The user is authenticated.
- `show_unread_badge` is true.
- The unread count is greater than zero.

The badge can fetch on mount and refresh after navigation to `/notifications` workflows update read state. This slice does not need real-time updates.

## Data Flow

1. Trigger execution creates notifications for resolved active recipients.
2. Before inserting a notification, backend checks whether the recipient has disabled in-app notifications.
3. The signed-in user sees the unread count in the navbar if their preferences allow badges.
4. The `/notifications` page reads and updates the same preferences.
5. Marking notifications read refreshes page state and the navigation badge state.

## Error Handling

Backend endpoints return:

- `401` for unauthenticated requests.
- `400` for invalid preference payloads.
- `404` only if the current authenticated user no longer exists.

Frontend should show existing page-level error handling for load failures and a compact save error near the preferences panel for update failures. Failed preference updates should not mutate local UI state permanently.

## Testing

Frontend:

- API tests for preference read/update endpoint paths and payloads.
- Notification page helper or component-level logic tests where existing lightweight test patterns make that practical.
- Module/navigation tests for the notification badge label/count behavior if the registry exposes the needed state.

Backend:

- Harness checks for preferences DTO defaults and update behavior where practical.
- Trigger notification creation should continue to pass existing validation, with preference filtering covered by service-level harness code if the current harness supports it.

## Documentation

Add `tasks/v4/007-notification-badges-and-preferences.md` and update:

- `tasks/v4/README.md`
- `docs/MASTER_PRD_FOR_AI.md`
- `docs/ROADMAP.md`
- `docs/TRIGGERS_AND_WORKFLOWS.md`
- `docs/DATA_MODEL.md`
- `docs/API_SPEC.md`

## Acceptance Criteria

- Backend exposes authenticated current-user notification preference read/update APIs.
- Preferences persist in PostgreSQL and default safely for users without a row.
- Trigger-created in-app notifications respect `in_app_enabled`.
- Navbar shows an unread notification badge when allowed by preferences.
- `/notifications` lets the current user update notification preferences.
- Existing notification read-state workflows still work.
- Available frontend tests/build and backend harness/build pass.
