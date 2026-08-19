# Task: Bounded Processing Jobs and Schedules

## Goal

Add durable, workspace-owned processing job definitions and runs for queued CSV record imports and protected record exports, with bounded execution, atomic scheduling/claims, and safe retry controls.

## Context

Read `AGENTS.md`, `docs/MASTER_PRD_FOR_AI.md`, `docs/ARCHITECTURE.md`, `docs/API_SPEC.md`, `docs/PERMISSIONS.md`, `docs/DATA_MODEL.md`, `docs/CREATOR_APP_SUPPORT_ROADMAP.md`, `docs/V8_FINALIZATION.md`, `docs/V10_START_HERE.md`, `tasks/v10/README.md`, and this task file.

V8 already provides synchronous CSV record import jobs with row results, permission-filtered CSV/JSON export jobs with protected artifacts, scheduled trigger contracts, atomic trigger claims, and integration logs. Those APIs are useful execution primitives but do not provide reusable named processing definitions, a bounded shared run queue, or safe scheduled export runs.

## Requirements

- Add a separate processing-jobs module; do not turn forms, reports, triggers, or the integrations page into a generic script runner.
- Persist workspace-owned soft-deletable job definitions and immutable-attempt run records in new normalized tables with concurrency stamps, audit metadata, and workspace query/write guards.
- Support exactly two typed job definition kinds in this task:
  - `csv_record_import`: target form, integration key, and existing CSV field mapping; manual runs supply one bounded CSV file.
  - `record_export`: existing `form_records` or `list_report` source metadata, CSV/JSON format, integration key, optional search, and a configured maximum row count.
- Reuse `RecordImportJobService` and `ExternalExportJobService` for execution, validation, permission filtering, row results, artifacts, protected downloads, audit logs, and integration logs. Store the resulting import/export job ID on the processing run rather than duplicating its detailed result model.
- Add no arbitrary commands, code, URLs, expressions, HTTP requests, connector operations, workflow/trigger invocations, record mutations beyond the existing CSV import boundary, or user-authored payload templates.
- Require `integrations.manage` for definition/run administration and recheck the concrete form/report permissions required by the reused import/export operation.
- Store a persistent initiating user ID for every definition. Scheduled execution must rehydrate that active user's current workspace membership and current permissions at run time; disabled/missing users, inactive memberships, or revoked source access fail closed.
- Creating a definition or queueing a run requires a persistent active workspace user. Bootstrap-only recovery identities may inspect and administer existing definitions but cannot own or execute asynchronous work that cannot safely rehydrate their transient identity.
- Allow manual runs for both kinds. A CSV import run accepts a bounded filename/content input using the existing CSV size/row/parser limits; an export run accepts no runtime payload.
- Store queued CSV input privately on the processing run, never in logs, audit metadata, list/detail DTOs, or error messages. Clear the raw input after terminal completion while retaining safe filename, byte count, checksum, and linked import-job metadata.
- Allow schedules only for `record_export`. Reuse the existing once/daily/weekly/monthly schedule semantics, timezone validation, interval/day bounds, and next-run calculation through a neutral shared scheduling component rather than coupling processing definitions to trigger entities.
- Store explicit retry policy metadata on definitions. Automatic/manual retry is supported only for failed `record_export` runs because export artifact generation is repeatable. CSV imports are single-attempt to prevent duplicate record creation after partial success.
- Bound retry policy to at most five attempts with delays from 30 seconds through 24 hours. Preserve the original run as the retry chain root and expose attempt/source IDs without copying sensitive inputs into metadata.
- Enforce one active run per definition. Manual requests return conflict while a pending/running run exists; the schedule worker skips and advances a due definition rather than enqueuing overlapping work.
- Atomically claim due definitions before enqueueing scheduled runs. Use a five-minute abandoned schedule-claim lease and compare the exact claimed timestamp/token when advancing `nextRunAt`.
- Atomically claim pending/due-retry runs with an opaque claim ID and five-minute lease. Every terminal update must be fenced by that claim ID so an expired worker cannot overwrite a newer attempt.
- Process at most five runs per worker pass and at most ten due definitions per scheduler pass. Make polling intervals configurable with conservative defaults.
- Keep export execution bounded. `maxRows` must be between 1 and 5,000. Refuse with a stable `source_limit_exceeded` failure before creating an artifact when the authorized result exceeds the configured bound; never silently truncate.
- Preserve deterministic source ordering while applying the bound. If the existing report execution path cannot enforce the bound without loading an unbounded source set, stop and review the report-query change rather than claiming bounded execution.
- Persist only sanitized stable error codes plus bounded operator-safe messages on processing runs. Do not persist stack traces, record values, CSV rows, artifact content, credentials, permission-policy details, or remote payloads in the run table.
- Add definition and run list/detail/create/update/delete/enable/disable/manual-run/retry APIs under `/api/processing-jobs`. List endpoints must be paginated and capped at 100 items per page.
- Return `404` rather than disclosing inaccessible source definitions. Use optimistic concurrency for definition updates and enable/disable operations.
- Write audit events for definition create/update/delete/enable/disable, manual run request, scheduled enqueue, retry request, and terminal run status. Worker claims and polling do not create audit noise.
- Add a Processing jobs section to `/integrations` for typed definition editing, schedule/retry controls, manual run/upload, paginated recent runs, safe status/error display, linked import/export detail, retry, and protected artifact download.
- Keep every control permission-aware and provide independent loading, empty, error, retry, conflict, and stale-concurrency states.
- Task 009 owns richer cross-job operational logs, aggregate metrics, and deduplicated failure notifications. Task 008 exposes only the minimal definition/run history required to operate this queue.

## Proposed API Contract

- `GET /api/processing-jobs?page=1&pageSize=25`
- `POST /api/processing-jobs`
- `GET /api/processing-jobs/{definitionId}`
- `PUT /api/processing-jobs/{definitionId}`
- `DELETE /api/processing-jobs/{definitionId}`
- `POST /api/processing-jobs/{definitionId}/enable`
- `POST /api/processing-jobs/{definitionId}/disable`
- `GET /api/processing-jobs/{definitionId}/runs?page=1&pageSize=25`
- `GET /api/processing-jobs/{definitionId}/runs/{runId}`
- `POST /api/processing-jobs/{definitionId}/runs` queues a manual export or accepts the bounded CSV import input.
- `POST /api/processing-jobs/{definitionId}/runs/{runId}/retry` requeues an eligible failed export run.

Definitions contain `name`, `kind`, typed `config`, optional export-only `schedule`, export-only `retryPolicy`, `isEnabled`, `nextRunAt`, and `concurrencyStamp`. Runs expose identifiers, source (`manual`, `scheduled`, or `retry`), status, attempts, safe timing/error metadata, and linked `recordImportJobId` or `externalExportJobId`; they never return raw CSV content or artifact bodies.

## Acceptance Criteria

- [x] Workspace-owned typed job definitions and fenced run records persist through a documented migration.
- [x] Only CSV record-import and protected record-export definitions are accepted; unknown properties and executable/script-like metadata are rejected.
- [x] Existing import/export services remain authoritative and processing runs link to their results rather than duplicating them.
- [x] Manual CSV inputs are bounded and private, never appear in projections/logs, and are cleared after terminal completion.
- [x] Export definitions support manual and once/daily/weekly/monthly scheduled runs using shared neutral schedule calculation.
- [x] Scheduling and worker execution use atomic bounded claims, leases, non-overlap, and fenced completion updates.
- [x] Export runs enforce a configured 1–5,000 row maximum without silent truncation or unbounded source loading.
- [x] Only failed exports can retry, with bounded policy/attempts and linked retry ancestry; partial CSV imports cannot retry.
- [x] Current user, membership, form/report, record-scope, field, and workspace-policy authorization is rechecked at execution time.
- [x] Definition/run APIs are paginated, concurrency-safe, non-disclosing, permission-protected, and audited.
- [x] `/integrations` provides responsive definition, schedule, manual-run/upload, recent-run, retry, and artifact operations without exposing sensitive input.
- [x] Task 009 logging/metrics/notification concerns and unsupported processing types remain out of scope.
- [x] API, architecture, data-model, permission, roadmap, and V10 documentation plus backend/frontend tests are complete.
- [x] Backend harness/build, frontend tests/build, authenticated PostgreSQL/API acceptance, worker claim/recovery acceptance, and `git diff --check` pass.

## Out of Scope

- EDI/XML/PDF/partner-specific transformations, SFTP pull/push, connector execution/testing, remote file deletion/archive, email delivery, or external API calls.
- Scheduled CSV imports, automatic or whole-file import retries, duplicate-record reconciliation, or import rollback.
- Arbitrary scripts, expressions, commands, URLs, payload templates, plugins, or a generic workflow/action engine.
- Bulk report actions, workflow starts, trigger actions, document generation, or processing related-record panels.
- Cross-job dashboards, Prometheus metrics, retention cleanup, alert routing, and failure notifications; these belong to Task 009 or later retention work.
- Distributed exactly-once guarantees. Claims, leases, fencing, non-overlap, and idempotent export retries provide bounded at-least-once worker safety.

## Tests

- Add backend validation tests for both typed configs, unknown properties, schedule compatibility, retry compatibility, bounds, persistent actor requirements, and source/form/report consistency.
- Add migration/model tests for workspace ownership, indexes, concurrency stamps, definition/run relationships, retry ancestry, claims, timestamps, and private input storage.
- Add authorization tests for `integrations.manage`, active user/membership rehydration, source permissions, record scopes, hidden fields, report access, policy denials, and non-disclosing cross-workspace IDs.
- Add worker tests for bounded claim batches, claim races, abandoned leases, fencing, non-overlap, due schedule advancement, disabled/deleted definitions, cancellation, and graceful shutdown.
- Add execution tests proving CSV input cleanup, linked import/export IDs, row-limit failure before artifact creation, protected downloads, safe errors, retry ancestry, attempt exhaustion, and import retry rejection.
- Add frontend API/helper/page coverage for typed editors, schedule/retry constraints, CSV upload, manual run, pagination, conflicts, stale updates, safe errors, retries, and artifact links.
- Exercise authenticated PostgreSQL/API acceptance for manual import/export, scheduled export, permission revocation before execution, failed export retry, claim recovery, bound failure, and complete fixture cleanup.

## Migration Notes

- Add `processing_job_definitions` and `processing_job_runs` with direct workspace ownership and foreign keys to the persistent initiating user plus optional linked form, report, import job, export job, and retry-source run.
- Index definitions by workspace/status/next-run and runs by workspace/definition/status/next-attempt/created date. Add a database-enforced partial uniqueness rule preventing more than one pending/running run per definition.
- Store typed configs, schedule, retry policy, and sanitized result metadata in JSONB where flexibility is appropriate; keep status, claim/fencing, attempts, ownership, timestamps, source links, and private queued input as explicit columns.
- Do not modify existing import/export job history or trigger definition/log tables. Extract only neutral scheduling calculation code needed by both trigger schedules and processing schedules.

## Review Decisions Proposed

- Task 008 coordinates existing import/export primitives; it does not replace their result models or security boundaries.
- CSV import is manual and single-attempt because replay after partial success is not safely idempotent.
- Export is the only scheduled/retryable kind in this slice and must fail rather than truncate above its configured bound.
- Scheduled execution acts as the persistent definition owner using current membership and permissions, never a timeless elevated service identity.
- Minimal run status belongs here; richer operational logs, metrics, and deduplicated alerts belong to Task 009.
