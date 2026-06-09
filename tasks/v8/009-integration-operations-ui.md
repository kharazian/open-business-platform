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

- [ ] Integration UI is permission-aware.
- [ ] API keys can be created/revoked/rotated without showing stored secrets.
- [ ] Integration logs are filterable and readable.
- [ ] Retry actions are explicit and auditable.
- [ ] Empty/error/loading states are clear.
- [ ] Documentation is updated if routes or commands change.
- [ ] Tests are added where practical.
- [ ] Relevant build/test commands are run.

## Out of Scope

- Public integration marketplace.
- Tenant-level integration policy UI.
- Custom connector builder.
- External database sync UI.
