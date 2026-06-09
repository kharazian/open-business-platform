# V8 Task 003: Public Record API Foundation

## Goal

Expose the first authenticated public/internal API endpoints for form records while reusing existing backend permissions, record scopes, and field rules.

## Context

Read:

- `docs/V8_START_HERE.md`
- `docs/API_SPEC.md`
- `docs/DATA_MODEL.md`
- `tasks/v8/001-api-keys-and-integration-auth.md`
- `tasks/v8/002-integration-logs-and-retry-foundation.md`
- `AGENTS.md`

## Requirements

- Require API key authentication for integration API calls.
- Support a minimal record create/read/list surface for selected forms.
- Reuse form view/create permissions and V3 record scopes.
- Reuse backend record validation.
- Do not expose hidden field values.
- Write audit or integration logs for sensitive operations.
- Keep contracts versioned and explicit.

## Acceptance Criteria

- [x] API key authenticated record endpoints exist.
- [x] Record create uses existing backend validation.
- [x] Record list/read respects record scopes.
- [x] Hidden fields are not returned to unauthorized integrations.
- [x] Integration/audit logs are written where appropriate.
- [x] Documentation is updated.
- [x] Tests are added where practical.
- [x] Relevant build/test commands are run.

## Implementation Notes

- Added explicit API key scopes `integrations.records.read` and `integrations.records.create`.
- Added versioned endpoints under `/api/integration/v1` for record list, read, and create.
- Record API calls require API key authentication and evaluate form permissions, record scopes, and hidden-field rules through the API key's linked `createdById` platform user.
- Record create reuses `RecordSubmissionService` and backend form schema validation.
- Successful list/read/create calls write integration logs without storing record values in log metadata. Creates also keep the existing record-created audit log.
- No database schema migration was needed for this task.

## Out of Scope

- Arbitrary query language.
- Cross-form joins.
- Bulk imports.
- Anonymous public access.
- Export/report APIs.
