# V9 Task 006: Backup, Restore Planning, And Administrative Export

## Status

Complete.

## Goal

Create protected, auditable workspace snapshot artifacts and validation-only restore plans without overwriting live data or exposing credentials.

## Scope

- Persist workspace-owned administrative backup jobs and restore plans.
- Support `configuration_only` and `full` JSON snapshots.
- Export workspace metadata, forms/versions, reports, dashboards, triggers, workflows/versions, print templates/versions, and records for full snapshots.
- Exclude users/passwords, reset tokens, SSO secrets, API-key hashes, webhook hashes, connector secret references, and previous artifact bodies.
- Add a versioned manifest with workspace ID, creation time, included modules, entity counts, artifact size, and SHA-256 checksum.
- Protect list/detail/create/download/plan endpoints with `backup.manage`; full snapshots also require effective `forms.manage_all` and complete form/record export access after policy evaluation.
- Audit snapshot creation, artifact download, and restore planning.
- Validate stored artifacts against checksum, manifest version, workspace target, supported modules, and current-ID conflicts.

## Out Of Scope

- Applying restore plans, overwriting live data, deletion, database-native dumps, encryption-key management, scheduling, remote storage, or uploaded foreign artifacts.
- Identity/credential backup, cross-workspace restore, partial merge execution, or rollback orchestration.

## Safety Rules

- Restore is validation-only in this task.
- Artifact downloads use authenticated endpoints and are audited.
- Artifact bodies are never returned by list/detail APIs.
- Full exports require workspace-wide form management, backup administration, and complete policy-filtered form/record export access.
- Snapshot generation is bounded to 25 MiB and fails closed if exceeded.
- Checksums are recomputed before download and restore planning.

## Acceptance Criteria

- [x] Backup jobs and restore plans are workspace-owned, indexed, concurrency-safe, and migration-documented.
- [x] Artifacts contain versioned manifests, counts, module lists, and SHA-256 checksums.
- [x] Secret/credential material and previous artifacts are excluded.
- [x] Full snapshots require `forms.manage_all` and backup administration.
- [x] Downloads are protected, checksum-verified, and audited.
- [x] Restore plans validate only and report conflicts/warnings without writes to business data.
- [x] Backend harness/build, migration consistency, frontend tests/build, and `git diff --check` pass.
- [x] API, data model, security, architecture, roadmap, master PRD, and V9 handoff docs are updated.
