# V8 Task 002: Integration Logs And Retry Foundation

## Goal

Create a shared integration log foundation so inbound and outbound integration work is observable before adding more integration surfaces.

## Context

Read:

- `docs/V8_START_HERE.md`
- `docs/API_SPEC.md`
- `docs/DATA_MODEL.md`
- `tasks/v8/001-api-keys-and-integration-auth.md`
- `tasks/v4/009-automatic-trigger-retry-queue.md`
- `tasks/v4/010-webhooks-retry-policies-scheduled-triggers.md`
- `AGENTS.md`

## Requirements

- Add typed integration log records for inbound and outbound attempts.
- Track status, direction, integration type, source, target entity, attempt count, timestamps, and sanitized error details.
- Store request/response metadata safely without leaking secrets or hidden field values.
- Add retry metadata for retryable failures.
- Keep retry execution explicit; do not silently replay actions.
- Add audit logs for manual retry or status-changing operations.

## Acceptance Criteria

- [x] Integration log contracts are typed.
- [x] Integration log persistence is documented.
- [x] Sensitive request/response data is redacted.
- [x] Retry metadata supports safe future retry workers/UI.
- [x] Manual retry operations are auditable if added.
- [x] Tests are added where practical.
- [x] Documentation is updated.
- [x] Relevant build/test commands are run.

## Implementation Notes

- Added `integration_logs` through EF Core migration `20260609210128_IntegrationLogs`.
- Added typed directions, integration types, statuses, DTOs, retry states, retry scheduler metadata helpers, and request/response metadata sanitization.
- Added management endpoints for listing logs, reading one log, and explicitly marking retryable failed logs for retry handling.
- Manual retry request marking writes an `integration_log_retry_requested` audit log entry. It does not replay integration actions or start a background retry worker.

## Out of Scope

- Building every integration type.
- Background retry worker for every failure class.
- Custom code execution.
- Arbitrary external database sync.
