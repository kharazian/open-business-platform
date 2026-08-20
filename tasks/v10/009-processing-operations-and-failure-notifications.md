# Task: Processing Operations and Failure Notifications

## Goal

Add searchable, payload-safe operational diagnostics and deduplicated in-app failure notifications for V10 processing jobs without turning audit history, integration logs, or the notification inbox into a generic logging system.

## Context

Read `AGENTS.md`, `docs/MASTER_PRD_FOR_AI.md`, `docs/ARCHITECTURE.md`, `docs/API_SPEC.md`, `docs/PERMISSIONS.md`, `docs/DATA_MODEL.md`, `docs/CREATOR_APP_SUPPORT_ROADMAP.md`, `docs/V10_START_HERE.md`, `tasks/v10/README.md`, `tasks/v10/008-bounded-processing-jobs-and-schedules.md`, and this task file.

Task 008 provides durable CSV-import/export definitions, bounded runs, safe scheduling, fenced workers, retry ancestry, minimal run errors, and linked authoritative import/export results. Operators can inspect one definition's recent runs, but they do not have a workspace-wide diagnostic stream, aggregate processing health, or deduplicated failure alerts.

## Requirements

- Add processing operational logs as a separate workspace-owned diagnostic model. Do not reuse audit logs, integration logs, trigger logs, application console logs, or processing-run rows as an interchangeable log store.
- Keep audit logs authoritative for security and business history. Operational logs describe execution health and must not record configuration changes already covered by audit.
- Persist platform-authored processing events only. This task supports a fixed event catalog for run queued, started, succeeded, failed, retry scheduled, retry exhausted, abandoned import failed closed, and schedule skipped because another run is active.
- Give every operational log a stable severity (`info`, `warning`, or `error`), event code, bounded platform-authored message, occurrence timestamp, definition ID, optional run ID, optional attempt metadata, optional safe error/result metadata, and an internal deterministic event key used only for idempotency.
- Never persist CSV rows/content, record values, artifact bodies, credentials, stack traces, exception text, permission-policy details, HTTP payloads, arbitrary parameters, or user-authored log messages. Allow only explicitly projected metadata such as run source, attempt/max attempts, stable error code, duration, result row count, and linked import/export job IDs.
- Write lifecycle logs only after the associated state transition wins its atomic/fenced update. Polling, unsuccessful claims, and routine lease checks must not create log noise.
- Preserve a coherent terminal transition: terminal run state, terminal operational log, retry scheduling decision, and eligible failure-notification records must commit atomically or be safely repeatable through database uniqueness constraints.
- Add a read-only workspace-wide operational log API with filters for definition, run, kind, severity, event code, stable error code, and bounded UTC date range. Paginate newest-first with deterministic ID ordering and cap pages at 100 rows.
- Add a bounded aggregate summary API over the same authorized processing scope. Default to the last 24 hours, limit custom ranges to 31 days, and return queued/running/succeeded/failed counts, retry scheduled/exhausted counts, schedule-skip count, and counts by the two supported processing kinds.
- Require `integrations.manage` for operational log and summary access. Apply workspace filters and return `404` for inaccessible definition/run identifiers rather than disclosing their existence.
- Do not expose logs for a definition whose concrete form/report source the caller can no longer inspect through the Task 008 authorization boundary. Do not return denial reasons or hidden source metadata.
- Extend processing job definitions with an optional typed failure-notification policy containing `isEnabled`, `includeOwner`, and explicit `recipientUserIds`. Default missing/legacy policies to disabled so the upgrade does not create unsolicited alerts.
- Limit explicit recipients to 25 unique persistent active users in the current workspace. Reject bootstrap identities, cross-workspace IDs, unknown properties, and configurations that enable notifications without at least one owner or explicit recipient.
- Provide a bounded recipient-options endpoint for the policy editor. It returns only active eligible current-workspace user IDs and display labels, requires `integrations.manage`, supports bounded search/pagination, and must not expose roles, email credentials, membership internals, or users from other workspaces.
- Revalidate recipients at delivery time. Send only to active workspace members whose current permissions allow processing-job administration; skip removed, inactive, or unauthorized recipients without leaking policy details.
- Create in-app alerts only for a terminal failure that will not receive another automatic retry: a failed single-attempt CSV import or an export retry chain whose automatic attempts are exhausted. Do not alert for an intermediate failure followed by an automatic retry.
- Deduplicate per recipient, processing definition, retry-chain root, and alert kind with a database-enforced key. Worker replay, overlapping delivery attempts, and repeated handling of the same terminal failure must not create duplicate notifications.
- A later manual retry remains part of the same retry chain and does not create a second alert for that chain. A separate manual/scheduled run has a new root and may create its own alert.
- Reuse the existing notification table, inbox, unread badge, read state, and `inAppEnabled` preference. Add only the nullable source/deduplication metadata needed for safe processing links and database-enforced deduplication.
- Processing notifications must use a trusted `ProcessingJobRun` source type and platform-derived links. Store safe IDs, job kind, attempt/max attempts, and stable error code only; never persist raw CSV input, exception details, or user-authored URLs in notification metadata.
- Keep notification preference semantics authoritative. `inAppEnabled = false` suppresses creation for that recipient; `showUnreadBadge` continues to affect display only.
- Make the notification list bounded and paginated while preserving the existing response's `items` property for frontend compatibility. The current-user-only ownership boundary and non-disclosing read operations remain unchanged.
- Extend the Processing jobs UI under `/integrations` with a failure-notification policy editor, workspace processing-health summary, and paginated/filterable operational log view. Keep per-definition run history as the detailed execution source.
- Add trusted notification navigation from a processing failure alert to the selected definition/run when the current user remains authorized. A missing/deleted/inaccessible target must fail safely without exposing source details.
- Provide independent loading, empty, error, retry, pagination, and stale-authorization states. Do not show raw metadata JSON or internal deduplication keys in the UI.
- Apply bounded technical retention to processing operational logs only: configurable from 7 through 365 days, default 90 days, with a daily cleanup batch of at most 500 rows. Do not delete audit logs, integration logs, processing runs, or notifications in this task.
- Emit payload-free structured application warnings and counters for notification suppression/deduplication and operational-log persistence failures. Do not add a second Prometheus endpoint or change API liveness semantics.

## Proposed API Contract

- `GET /api/processing-operations/logs?page=1&pageSize=25&definitionId=&runId=&kind=&severity=&eventCode=&errorCode=&from=&to=`
- `GET /api/processing-operations/summary?from=&to=`
- `GET /api/processing-operations/notification-recipients?page=1&pageSize=25&search=`
- Existing processing definition create/detail/update DTOs add `failureNotificationPolicy`:

```json
{
  "failureNotificationPolicy": {
    "isEnabled": true,
    "includeOwner": true,
    "recipientUserIds": ["00000000-0000-0000-0000-000000000000"]
  }
}
```

Operational log items return only safe typed fields:

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "definitionId": "00000000-0000-0000-0000-000000000000",
  "runId": "00000000-0000-0000-0000-000000000000",
  "kind": "record_export",
  "severity": "error",
  "eventCode": "run_failed",
  "message": "Processing run failed after its configured attempts.",
  "attempt": 3,
  "maxAttempts": 3,
  "errorCode": "source_limit_exceeded",
  "durationMilliseconds": 840,
  "recordImportJobId": null,
  "externalExportJobId": null,
  "occurredAt": "2026-08-20T15:30:00Z"
}
```

The exact DTO names may follow existing processing-module conventions. Unknown filters return validation errors rather than being ignored. Notification list pagination may add `page`, `pageSize`, and `totalCount` alongside `items`.

## Acceptance Criteria

- [x] Workspace-owned processing operational logs persist separately from audit, integration, trigger, and run history through a documented migration.
- [x] A fixed, severity-typed event catalog records only successful lifecycle transitions and safe bounded metadata.
- [x] Worker races, expired claims, and replay cannot duplicate terminal logs, retry-exhausted events, or notifications.
- [x] Workspace log search is permission-aware, source-aware, bounded by date/page limits, deterministically ordered, and non-disclosing.
- [x] Aggregate processing health uses the same authorized scope and bounded time range without unbounded in-memory loading.
- [x] Definition notification policies validate enabled state, owner inclusion, unique recipient limits, persistent users, workspace membership, and unknown properties.
- [x] The policy editor can query only bounded, safe, eligible current-workspace recipient options.
- [x] Only terminal non-retrying failures alert; intermediate automatic-retry failures do not.
- [x] Failure notifications are deduplicated per recipient and retry chain by a database constraint, including after a later manual retry.
- [x] Existing notification preferences, current-user ownership, read state, and unread badges remain authoritative, and notification listing is paginated.
- [x] Notification bodies and metadata contain only safe platform-authored details and trusted processing IDs; no raw inputs, values, artifacts, stack traces, or policy details are stored.
- [x] `/integrations` provides failure-policy editing, bounded health summaries, searchable operational logs, and safe links to authorized run details.
- [x] Operational-log retention is configurable and cleaned in bounded batches without deleting audit, integration, run, or notification history.
- [x] API, architecture, data-model, permission, roadmap, and V10 documentation plus backend/frontend tests are complete.
- [x] Backend harness/build, frontend tests/build, authenticated PostgreSQL/API acceptance, concurrent deduplication acceptance, cleanup acceptance, and `git diff --check` pass.

## Out of Scope

- A generic log ingestion API, user-authored log statements, arbitrary parameters, custom code line tracing, stack traces, record payload snapshots, or raw exception persistence.
- Cross-module normalization of trigger, workflow, integration, audit, and application logs into one table or search endpoint.
- Email, SMS, Slack, Teams, push, webhooks, paging/on-call integrations, escalation rules, quiet hours, delivery receipts, or external alert routing.
- Role/group/department recipient selectors, per-error routing rules, custom notification bodies, user-authored URLs, templates, or expressions.
- Alert acknowledgement/assignment, incident management, manual alert resend, or notification deletion.
- Prometheus endpoint expansion, distributed tracing, external log shipping, dashboards outside the processing-jobs workspace, or certification claims.
- Retention execution for audit logs, integration logs, processing runs, import/export results, artifacts, or notifications.
- New processing kinds, scheduled imports, unsafe import replay, arbitrary connector execution, or changes to Task 008's bounded execution semantics.

## Tests

- Add backend validation tests for policy defaults, enabled recipient requirements, uniqueness/bounds, unknown properties, bootstrap/cross-workspace/inactive users, and stale recipient authorization.
- Add migration/model tests for operational-log ownership/indexes, notification deduplication uniqueness, definition policy storage, foreign keys, and delete behavior.
- Add worker tests for event ordering, fenced terminal writes, automatic-retry suppression, exhausted-chain alerts, single-attempt import alerts, manual-retry ancestry, replay, and concurrent deduplication races.
- Add payload-safety tests proving raw CSV, record values, artifact bodies, credentials, exception/stack text, and policy details never reach logs, notification metadata, API DTOs, or application log messages.
- Add authorization tests for `integrations.manage`, current workspace, source access revocation, inaccessible IDs, recipient membership/permission changes, and user notification ownership.
- Add query and bounds tests for filters, deterministic pagination, 31-day summary limits, aggregate counts, notification pagination, and no unbounded materialization.
- Add cleanup tests for retention bounds, batches of at most 500, cancellation, workspace safety, and preservation of audit/integration/run/notification rows.
- Add frontend API/helper/page tests for policy editing, summary states, log filters/pagination, safe event rendering, notification navigation, inaccessible targets, and stale authorization.
- Exercise authenticated PostgreSQL/API acceptance for a successful run, retrying failure, exhausted failure, recipient preference suppression, concurrent duplicate delivery, log search/summary, notification link, and fixture cleanup.

## Migration Notes

- Add `processing_operational_logs` with direct workspace ownership and foreign keys to the processing definition plus optional run. Keep severity, event code, occurrence time, attempt/error fields, safe result links, and a bounded opaque event key queryable; use JSONB only for a small allowlisted metadata projection if required.
- Add a nullable failure-notification policy JSONB column to `processing_job_definitions`. Missing values normalize to a disabled policy.
- Add a nullable bounded deduplication key to `notifications` and a unique partial index over workspace, user, and key where the key is present. Existing notifications remain unchanged.
- Add a unique workspace/event-key constraint so retrying the same persistence step is idempotent. Recommended read indexes cover workspace/occurred time, workspace/severity/occurred time, definition/occurred time, run/event code, and retention scans.
- Define delete behavior explicitly: deleting a definition remains soft-delete; operational logs/runs remain query-filtered workspace history. Do not cascade-delete diagnostic history through the definition API.

## Review Decisions

- Task 009 is limited to processing-job operations. It does not pretend that audit, trigger, workflow, and integration logs share one diagnostic schema.
- Alerts fire only when no automatic retry remains, which avoids noisy intermediate-failure notifications.
- Legacy and newly upgraded definitions do not alert until an administrator explicitly enables a notification policy.
- Explicit users plus optional owner are sufficient for this slice; role/group routing and external delivery need separate semantics.
- A nullable database-enforced notification deduplication key provides replay safety without introducing a parallel alert table.
- Operational health is an authenticated bounded API/UI summary, not a new public monitoring or liveness contract.
- Processing operational logs receive technical retention; authoritative audit and execution history remain untouched.
