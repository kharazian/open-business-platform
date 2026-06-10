# V8 Task 009: Integration Operations UI

## Goal

Add an operational UI for integration administrators to manage API keys, view integration logs, and retry eligible failed integration work.

## Context

Read:

- `docs/V8_START_HERE.md`
- `docs/API_SPEC.md`
- `tasks/v8/001-api-keys-and-integration-auth.md`
- `tasks/v8/002-integration-logs-and-retry-foundation.md`
- Relevant V8 implementation tasks already completed.
- `AGENTS.md`

## Requirements

- Add a dense `/integrations` management surface.
- Manage API key metadata, creation, revocation, and rotation.
- View integration logs with filters by type, status, direction, source, and time.
- Retry only eligible failed operations.
- Show sanitized errors and retry results.
- Reuse shared UI components and existing permission patterns.

## Acceptance Criteria

- [x] Integration UI is permission-aware.
- [x] API keys can be created/revoked/rotated without showing stored secrets.
- [x] Integration logs are filterable and readable.
- [x] Retry actions are explicit and auditable.
- [x] Empty/error/loading states are clear.
- [x] Documentation is updated if routes or commands change.
- [x] Tests are added where practical.
- [x] Relevant build/test commands are run.

## Implementation Notes

- Added a permission-protected `/integrations` route registered through the platform module registry.
- Added frontend integration API helpers for API key list/create/revoke/rotate, log list, and explicit retry request.
- Added a dense operations page with API key lifecycle controls, one-time raw key display after create/rotate, integration log filters, log detail metadata, and retry buttons only for eligible failed logs.
- Added focused frontend tests for API helpers, log filtering/retry eligibility, and module registration.

## Out of Scope

- Public integration marketplace.
- Tenant-level integration policy UI.
- Custom connector builder.
- External database sync UI.
