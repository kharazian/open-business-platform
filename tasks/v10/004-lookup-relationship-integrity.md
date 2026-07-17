# Task: Lookup Relationship Integrity

## Goal

Make record lookups durable relationships with authoritative source/target metadata, safe selection rules, and explicit delete/archive behavior.

## Context

Read `AGENTS.md`, `docs/MASTER_PRD_FOR_AI.md`, `docs/ARCHITECTURE.md`, `docs/API_SPEC.md`, `docs/DATA_MODEL.md`, `docs/V10_START_HERE.md`, and `tasks/v10/README.md`.

Lookup UUIDs currently live only in record JSON. Validation checks a target at submission time, but deletes can leave broken references and later relationship features have no indexed edge model.

## Requirements

- Add workspace-owned `record_relationships` for one canonical edge per source record and lookup field.
- Store source record/form/form-version/field plus target record/form and audit timestamps, with restrictive foreign keys and a unique source-record/field index.
- Synchronize relationship rows inside record create/edit transactions after permission-safe lookup validation.
- Remove outgoing edges when a source record is soft-deleted.
- Restrict deletion of a target record while any active source record references it; return a non-disclosing `409` with a safe reference count.
- Detect legacy JSON-only lookup references during deletion so pre-migration records are protected without rewriting their JSON or immutable schemas.
- Require the target form to be currently published for new selections and lookup option search.
- Preserve and display existing selections when a target form later becomes archived; unrelated edits must not fail solely because an unchanged relationship is now archived.
- Continue hiding inaccessible target existence behind the existing field-specific unknown/unavailable error.
- Keep hidden lookup fields and inaccessible source records out of option, display, and relationship APIs.
- Expose no generic relationship mutation endpoint; edges are derived from validated record values.
- Document delete/archive semantics and migration behavior.

## Acceptance Criteria

- [ ] Relationship metadata is workspace-owned, indexed, and protected by restrictive source/target foreign keys.
- [ ] Record create/edit synchronizes lookup edges atomically and removes replaced/cleared edges.
- [ ] Concurrent updates cannot create duplicate edges for one source record/field.
- [ ] Referenced target deletion returns `409`; unreferenced and source-record deletion still succeeds.
- [ ] Legacy JSON-only lookup references also restrict target deletion.
- [ ] Archived/draft source forms cannot supply new lookup selections or options.
- [ ] Unchanged existing selections remain editable/displayable after source form archival.
- [ ] Permission and hidden-field behavior remains non-disclosing.
- [ ] Existing schemas, versions, and record JSON remain compatible.
- [ ] Migration/API/data-model documentation and tests are complete.
- [ ] Backend harness/build, frontend tests/build, migration consistency, PostgreSQL/API acceptance, and `git diff --check` pass.

## Out of Scope

- Cascade delete, automatic clearing, polymorphic targets, many-to-many/multi-select lookups, or cross-workspace relationships.
- Generic graph APIs, nested report traversal, or related-record UI; those use this foundation in later tasks.
- Backfilling or mutating historical record JSON.

## Tests

- Add EF model, edge extraction/synchronization, archive behavior, delete restriction, and legacy detection tests.
- Apply the migration to clean PostgreSQL and exercise create, replacement, clear, delete restriction, and source deletion through authenticated APIs.

## Migration Notes

- Add `record_relationships` with workspace, source/target form and record indexes plus unique `(workspace_id, source_record_id, source_field_id)`.
- Do not backfill record JSON. New mutations materialize canonical edges; delete restriction scans legacy JSON-only references when needed.

## Notes

- The record's immutable form version defines which JSON members are lookups.
- Target record IDs remain the source value; relationship rows are derived integrity/index metadata.
- Implement only this task.
