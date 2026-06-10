# V8 Task 004: Incoming Webhook Listeners

## Goal

Add named incoming webhook listeners that can safely create or update records through typed mappings.

## Context

Read:

- `docs/V8_START_HERE.md`
- `docs/API_SPEC.md`
- `docs/DATA_MODEL.md`
- `tasks/v8/001-api-keys-and-integration-auth.md`
- `tasks/v8/002-integration-logs-and-retry-foundation.md`
- `tasks/v4/010-webhooks-retry-policies-scheduled-triggers.md`
- `AGENTS.md`

## Requirements

- Define persisted webhook listener configurations.
- Authenticate webhook calls with API key or listener secret.
- Map inbound payload fields to one target form.
- Validate generated record values through existing record validation.
- Support create first; update only if a safe lookup key is explicitly configured.
- Log every inbound attempt.
- Avoid custom code transforms.

## Acceptance Criteria

- [x] Webhook listener contracts are typed.
- [x] Listener secrets are stored safely.
- [x] Inbound requests are authenticated.
- [x] Payload mappings are validated before activation.
- [x] Record creation uses existing validation and permissions.
- [x] Integration logs capture success/failure.
- [x] Documentation is updated.
- [x] Tests are added where practical.
- [x] Relevant build/test commands are run.

## Implementation Notes

- Added `incoming_webhook_listeners` through EF Core migration `20260610130924_IncomingWebhookListeners`.
- Added typed listener contracts for create/upsert actions, API-key/listener-secret auth modes, field mappings, validation errors, and receive responses.
- Listener secrets are generated with an `obp_wh_` prefix and stored only as hashes; raw secrets are returned only on create/rotate.
- Management endpoints live under `/api/integrations/webhooks` and require cookie auth plus `integrations.manage`.
- Inbound listener execution lives at `POST /api/integration/v1/webhooks/{listenerKey}` and authenticates with either an API key carrying `integrations.webhooks.receive` or `X-OBP-Webhook-Secret`, according to the listener's configured auth mode.
- Record creation and safe-lookup upsert reuse existing record submission/mutation services, backend permissions, validation, audit logs, and trigger dispatch.
- Every inbound success or failure writes an `integration_logs` entry with type `webhook` and source `IncomingWebhookListener`; request metadata stores operational listener details, not raw payload values.

## Out of Scope

- Anonymous webhooks.
- Arbitrary JavaScript/custom transforms.
- Cross-form writes.
- Complex idempotency guarantees beyond a conservative first slice.
