# V8 Task 011: Transactional Trigger Event Outbox

## Goal

Prevent committed record changes from losing their trigger events when dispatch is temporarily unavailable.

## Context

Read:

- `docs/MASTER_PRD_FOR_AI.md`
- `docs/TRIGGERS_AND_WORKFLOWS.md`
- `docs/DATA_MODEL.md`
- `tasks/v1/005-record-submission.md`
- `tasks/v4/001-trigger-engine-foundation.md`
- `tasks/v8/010-production-hardening.md`
- `AGENTS.md`

## Requirements

- Persist record trigger events in the same PostgreSQL transaction as the record mutation.
- Cover record creation, edits, field changes, assignments, direct status changes, workflow starts/transitions, and approval-completed transitions.
- Claim pending events atomically and recover abandoned claims after a bounded lease.
- Retry infrastructure failures with bounded exponential backoff and retain exhausted messages as dead letters.
- Keep trigger action failures in the existing trigger log and automatic retry system.
- Do not expose outbox payloads through public APIs.

## Acceptance Criteria

- [x] A record mutation and its event message commit or roll back together.
- [x] Record and workflow mutation services no longer dispatch trigger events after commit.
- [x] Concurrent workers cannot claim the same outbox message.
- [x] Abandoned claims become eligible after the documented lease.
- [x] Failed deliveries retry with a maximum attempt count and become dead letters when exhausted.
- [x] EF migrations and architecture/data-model documentation are updated.
- [x] Focused backend tests are added.
- [x] Backend harness/build and migration checks pass.

## Status

Complete. Migration `20260715180727_TransactionalTriggerEventOutbox` adds the internal outbox table. All user/API record mutations and workflow-driven status mutations now stage record trigger events on the same EF Core context before commit. The hosted worker uses atomic claim updates with unique fencing ids, a five-minute abandoned-claim lease, five bounded delivery attempts, exponential backoff, and retained dead-letter rows.

## Out of Scope

- Exactly-once delivery to external providers.
- A public or administrative outbox API/UI.
- Replacing the existing per-trigger action retry queue.
- A distributed message broker.
