# V8 Task 001: API Keys And Integration Auth

## Goal

Add a secure API key foundation for integrations before any new public/internal API, webhook, import, or export endpoint is exposed.

## Context

Read:

- `docs/V8_START_HERE.md`
- `docs/MASTER_PRD_FOR_AI.md`
- `docs/API_SPEC.md`
- `docs/DATA_MODEL.md`
- `tasks/v8/README.md`
- `AGENTS.md`

## Requirements

- Store API keys hashed; never persist raw key material.
- Return the raw key only once at creation.
- Support active/revoked states and last-used metadata.
- Associate keys with a stable integration identity and optional created-by user.
- Add backend authentication plumbing for API key requests.
- Keep scopes conservative and typed.
- Add audit logs for create, revoke, and rotate actions.
- Do not expose record/report data in this task.

## Acceptance Criteria

- [x] API key contracts are typed.
- [x] API keys are stored hashed.
- [x] Raw key material is only returned once.
- [x] Revoked/inactive keys cannot authenticate.
- [x] Last-used metadata is updated safely.
- [x] Management endpoints require backend permissions.
- [x] Documentation is updated.
- [x] Tests are added where practical.
- [x] Relevant build/test commands are run.

## Implementation Notes

- Added `integration_api_keys` through EF Core migration `20260609203933_IntegrationApiKeys`.
- Added `integrations.manage` as the backend permission for API key management.
- Added `IntegrationApiKey` domain/model mapping, typed contracts, conservative scopes, hashing/generation helpers, management service, module endpoints, and `IntegrationApiKey` auth scheme.
- Current API-key auth plumbing supports `Authorization: Bearer <rawKey>` and `X-OBP-API-Key: <rawKey>` for future integration endpoints. No record/report data endpoints were added in this task.

## Out of Scope

- Public record APIs.
- Incoming webhook listeners.
- Import/export jobs.
- External database sync.
- Custom code execution.
