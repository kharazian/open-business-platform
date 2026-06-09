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

- [ ] Webhook listener contracts are typed.
- [ ] Listener secrets are stored safely.
- [ ] Inbound requests are authenticated.
- [ ] Payload mappings are validated before activation.
- [ ] Record creation uses existing validation and permissions.
- [ ] Integration logs capture success/failure.
- [ ] Documentation is updated.
- [ ] Tests are added where practical.
- [ ] Relevant build/test commands are run.

## Out of Scope

- Anonymous webhooks.
- Arbitrary JavaScript/custom transforms.
- Cross-form writes.
- Complex idempotency guarantees beyond a conservative first slice.
