# V8 Task 014: Automation Health and Metrics

## Goal

Make trigger-event delivery failures automatically visible to deployment health checks and monitoring systems without exposing event payloads or hidden record values.

## Context

Read:

- `docs/MASTER_PRD_FOR_AI.md`
- `docs/TRIGGERS_AND_WORKFLOWS.md`
- `docs/SECURITY_MODEL.md`
- `docs/ARCHITECTURE.md`
- `tasks/v8/012-trigger-outbox-operations.md`
- `AGENTS.md`

## Requirements

- Keep the existing `/health` liveness endpoint independent from automation backlog.
- Add an automation health endpoint that reports healthy/degraded/unhealthy without returning message data.
- Make pending-age and dead-letter warning thresholds configurable.
- Export payload-free Prometheus text metrics for outbox counts, retry backlog, and oldest pending age.
- Protect production metrics with an explicit bearer token and support disabling the endpoint.
- Emit structured warning logs with form/message identifiers but no payload or record values.
- Avoid new monitoring package dependencies.

## Acceptance Criteria

- [x] Automation backlog can degrade automation health without failing API liveness.
- [x] Database/query failure makes automation health unhealthy.
- [x] Metrics contain bounded aggregate series and no form/message/payload labels.
- [x] Production metrics access is denied without a configured valid token.
- [x] Dead-letter/delayed logs include form/message IDs and exclude payload JSON.
- [x] Configuration and deployment documentation are updated.
- [x] Focused backend tests and build checks pass.

## Status

Complete. `/health` remains independent API liveness, while `/health/automation` reports database-backed healthy/degraded/unhealthy automation state without message details. `/metrics` exposes bounded payload-free Prometheus aggregates and requires a configured bearer token outside development. Warning thresholds and monitor cadence are environment-configurable, and structured warnings identify form/message ids without reading payload JSON.

## Out of Scope

- Installing or operating Prometheus/Grafana.
- Sending alerts directly to Slack, email, or PagerDuty.
- Per-form metric labels.
- Making automation backlog fail API liveness.
