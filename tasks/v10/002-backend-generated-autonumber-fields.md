# Task: Backend-Generated Autonumber Fields

## Goal

Add immutable, human-readable autonumber fields whose values are allocated atomically by PostgreSQL during record creation.

## Context

Read:

- `AGENTS.md`
- `docs/MASTER_PRD_FOR_AI.md`
- `docs/ARCHITECTURE.md`
- `docs/API_SPEC.md`
- `docs/DATA_MODEL.md`
- `docs/V10_START_HERE.md`
- `tasks/v10/README.md`

Structured addresses established the first V10 composite value without relational state. Autonumbers are different: concurrency-safe allocation requires authoritative per-workspace, per-form, per-field state and must participate in the record creation transaction.

## Requirements

- Add an `autonumber` field type to matching frontend and backend schema contracts.
- Define bounded configuration for optional `prefix`, optional `suffix`, `startAt`, and `padding`.
- Reject invalid configuration, including negative values, excessive padding, overly long prefixes/suffixes, and configurations whose maximum formatted value can exceed the record-value limit.
- Add a workspace-owned sequence table keyed uniquely by form and field ID, with the next numeric value and audit timestamps.
- Allocate numbers atomically in PostgreSQL inside the existing record-submission transaction.
- Store the final formatted string in record `values_json`, preserving the published `form_version_id` used at creation.
- Ignore field defaults and prevent submitted record-create payloads from supplying autonumber values.
- Preserve existing autonumber values during record edits and reject attempts to change them.
- Render autonumbers as read-only in preview, entry, and record-edit modes; display them normally in lists, details, reports, CSV, print, lookup labels, triggers, and workflows.
- Keep gaps acceptable after rolled-back or failed business operations only if PostgreSQL allocation semantics require them; never reuse an issued number.
- Document migration and operational behavior.

## Acceptance Criteria

- [x] Draft/publish validation accepts bounded autonumber configuration and rejects invalid configuration.
- [x] Concurrent record creation cannot allocate duplicate values for the same workspace/form/field.
- [x] Separate forms, workspaces, and field IDs have independent counters.
- [x] The configured starting value, prefix, suffix, and padding produce deterministic strings.
- [x] Create payloads containing an autonumber value are rejected with a field-specific validation error.
- [x] Record edits preserve the original number and reject changes.
- [x] Builder, renderer, record, report, CSV, print, lookup, trigger, and workflow paths treat the generated value as ordinary display text.
- [x] Existing schemas and records remain compatible.
- [x] Migration, API, and data-model documentation are updated.
- [x] Backend harness/build, frontend tests/build, migration consistency, and `git diff --check` pass.

## Out of Scope

- Reclaiming, renumbering, or reusing issued values.
- Reset schedules, date-based counters, random identifiers, or custom expressions.
- Cross-form or cross-workspace shared sequences.
- User-editable generated numbers.
- Bulk backfilling historical records that predate the field.
- Exposing sequence-management endpoints.

## Tests

- Add backend configuration, formatting, client-write rejection, immutability, and allocation tests.
- Add a PostgreSQL-backed concurrency acceptance check where practical.
- Add frontend builder, renderer, and readonly-value tests.
- Verify legacy schema version 1 definitions remain valid.

## Migration Notes

- Migration `20260717191123_BackendGeneratedAutonumbers` adds a workspace-owned sequence table with restrictive workspace/form foreign keys.
- The migration adds a unique `(workspace_id, form_id, field_id)` index.
- Do not modify historical record JSON or published form versions.

## Notes

- Sequence allocation and record persistence must share one database transaction.
- A form may contain multiple independent autonumber fields.
- Formatting belongs to the immutable published schema; counter state belongs to PostgreSQL.
- A clean PostgreSQL acceptance run allocated 32 concurrent values with 32 distinct results spanning `1000` through `1031`; the stored next value was `1032`.
- Implement only this task.
