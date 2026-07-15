# V8 Task 013: Outbound Webhook Idempotency

## Goal

Give webhook receivers a stable delivery key so repeated attempts of the same trigger event/action can be deduplicated safely.

## Context

Read:

- `docs/MASTER_PRD_FOR_AI.md`
- `docs/TRIGGERS_AND_WORKFLOWS.md`
- `docs/API_SPEC.md`
- `tasks/v4/010-webhooks-retry-policies-scheduled-triggers.md`
- `tasks/v8/011-transactional-trigger-event-outbox.md`
- `AGENTS.md`

## Requirements

- Derive a deterministic opaque key from the trigger, action, and original event identity.
- Reuse the same key for manual/automatic action retries and outbox redelivery.
- Send the key in the platform-owned `Idempotency-Key` request header.
- Include the key in default webhook bodies and safe trigger action result metadata.
- Prevent user-authored headers from overriding the platform-owned key.
- Keep custom webhook bodies supported.

## Acceptance Criteria

- [x] The same trigger/action/event produces the same key across attempts.
- [x] Different actions or event occurrence times produce different keys.
- [x] Every outbound trigger webhook sends exactly one platform-generated idempotency header.
- [x] Default payloads and action results expose the same key.
- [x] Validation rejects a user-authored `Idempotency-Key` header.
- [x] Focused backend/frontend tests and builds pass.
- [x] API and automation documentation are updated.

## Status

Complete. Outbound trigger webhooks now carry one deterministic `Idempotency-Key` derived from the trigger, action, and original event identity. The key remains stable when the saved event context is retried or redelivered, appears in default payload/result metadata, and cannot be overridden through custom headers. Receivers remain responsible for storing keys and returning an idempotent response.

## Out of Scope

- Receiver-side deduplication storage.
- Exactly-once email delivery.
- Provider-specific webhook signing.
- Automatic deduplication guarantees from third-party systems.
