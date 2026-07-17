# V9 Task 005: Data Retention And Legal Hold Foundation

## Status

Complete.

## Goal

Define auditable workspace retention policies and legal holds with safe, payload-free dry-run evaluation before any destructive retention executor exists.

## Scope

- Persist workspace-owned retention policies for records, audit logs, and integration logs.
- Configure retention age, enabled state, optional form scope for records, priority, and optimistic concurrency.
- Persist workspace-owned legal holds for one typed entity ID with reason, placed/released audit metadata, and concurrency.
- Add `retention.manage` permission-protected management APIs.
- Evaluate retention candidates in the database and return aggregate count plus at most 100 IDs.
- Exclude active legal holds from every dry-run.
- Audit policy creation/update, legal-hold placement/release, and dry-run execution.

## Out Of Scope

- Deletion, anonymization, archival, scheduled execution, or automatic policy enforcement.
- Restoring deleted data, backup orchestration, legal discovery exports, field-level policies, or arbitrary JSON conditions.
- Retention for identity, permission, workflow, notification, trigger definition, print template, or integration credential tables.

## Safety Rules

- Dry-run is the only execution mode in this task.
- Results contain counts and IDs only, never record values, audit metadata, integration payloads, or errors.
- Retention age is bounded from 1 through 36500 days.
- Record policies may target all forms or one form in the active workspace.
- Active legal holds always win over retention eligibility.
- Released holds remain as immutable historical rows and cannot be reactivated.
- Every mutation and dry-run is audited.

## Acceptance Criteria

- [x] Retention policies and legal holds are workspace-owned, indexed, concurrency-safe, and migration-documented.
- [x] Management and dry-run endpoints require `retention.manage`.
- [x] Record, audit-log, and integration-log dry-runs use database-side age filters.
- [x] Active legal holds are excluded and released holds retain history.
- [x] Dry-run responses are payload-free and bounded.
- [x] No destructive retention behavior is introduced.
- [x] Backend harness/build, migration consistency, frontend tests/build, and `git diff --check` pass.
- [x] API, data model, security, architecture, roadmap, master PRD, and V9 handoff docs are updated.
