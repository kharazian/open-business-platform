# V8 Task 010: Production Hardening

## Goal

Harden the completed V8 integration and automation foundation against lost updates, duplicate scheduled work, repeated trigger side effects, and unaudited export artifact access.

## Context

Read:

- `docs/MASTER_PRD_FOR_AI.md`
- `docs/V8_FINALIZATION.md`
- `docs/SECURITY_MODEL.md`
- `docs/TRIGGERS_AND_WORKFLOWS.md`
- `tasks/v4/009-automatic-trigger-retry-queue.md`
- `tasks/v8/006-external-export-jobs.md`
- `tasks/v8/007-scheduled-automation-expansion.md`
- `AGENTS.md`

## Requirements

- Enforce optimistic concurrency in PostgreSQL through EF Core concurrency tokens.
- Return `409 Conflict` instead of an unhandled error when a concurrent write loses.
- Claim scheduled triggers atomically before executing them and recover abandoned claims after a bounded lease.
- Preserve completed trigger actions when retrying a partially failed execution.
- Serve export artifact content only through the permission-protected, audited artifact endpoint.
- Keep all existing V8 permission, hidden-field, audit, and integration-log behavior.

## Acceptance Criteria

- [x] Concurrent updates using the same stale concurrency stamp cannot both succeed.
- [x] Concurrent scheduler workers cannot claim the same scheduled run.
- [x] Abandoned scheduled claims become eligible after the documented lease duration.
- [x] A retry skips actions already recorded as successful for its source execution.
- [x] Export create/detail DTOs do not contain artifact content.
- [x] Export artifact downloads remain permission-protected and audited.
- [x] EF migrations and architecture/API/data-model documentation are updated.
- [x] Focused backend/frontend tests are added.
- [x] Backend harness/build and frontend test/build commands pass.

## Status

Complete. Migration `20260715174713_V8ProductionHardening` adds the scheduler lease column, and EF concurrency stamps are now update predicates across audited aggregate roots. Trigger retry results carry completed-action checkpoints, role-permission aggregate updates use the parent role stamp, and export artifact bodies are available only through the audited download endpoint.

## Out of Scope

- V9 workspace or tenant ownership.
- Provider-backed connector secret storage.
- A full transactional record-event outbox.
- Moving import/export execution to a distributed job service.
- Exactly-once delivery guarantees from external email or webhook providers.
