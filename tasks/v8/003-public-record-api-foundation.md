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

- [ ] API key authenticated record endpoints exist.
- [ ] Record create uses existing backend validation.
- [ ] Record list/read respects record scopes.
- [ ] Hidden fields are not returned to unauthorized integrations.
- [ ] Integration/audit logs are written where appropriate.
- [ ] Documentation is updated.
- [ ] Tests are added where practical.
- [ ] Relevant build/test commands are run.

## Out of Scope

- Arbitrary query language.
- Cross-form joins.
- Bulk imports.
- Anonymous public access.
- Export/report APIs.
