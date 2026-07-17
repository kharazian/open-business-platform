# Task: Nested Report Relationships

## Goal

Let list reports select, filter, search, sort, display, print, and export fields from one lookup-related record without exposing inaccessible records or hidden fields.

## Context

Read `AGENTS.md`, `docs/MASTER_PRD_FOR_AI.md`, `docs/ARCHITECTURE.md`, `docs/API_SPEC.md`, `docs/DATA_MODEL.md`, `docs/CREATOR_APP_SUPPORT_ROADMAP.md`, `docs/V10_START_HERE.md`, and `tasks/v10/README.md`.

V10 task 004 added canonical lookup edges and explicit relationship lifecycle rules. List reports still understand only fields on their root form, so operational views cannot show values such as `Parent Order > Order Number`.

## Requirements

- Extend list-report field references with a canonical dotted key: `{lookupFieldId}.{targetFieldId}`.
- Support exactly one lookup hop in this task; reject deeper, malformed, cyclic, unknown, or non-lookup paths.
- Keep existing root field IDs and schema-version-1 report configs compatible.
- Add a permission-filtered report field catalog for the builder, including eligible related fields with relationship-aware labels and typed metadata.
- Require the root lookup field to be visible and the caller to have target-form view access before exposing related field metadata.
- Exclude hidden target fields from builder options and execution; do not expose target record IDs as a fallback value.
- Resolve related values from stored lookup IDs while enforcing target-form access, target record scope, soft-delete state, and the target field rules for the current caller.
- Preserve existing lookup values when the target form is archived; archival alone must not break an existing report.
- Apply saved filters, runtime filters, search, saved sort, runtime sort, table display, print data, and CSV export to permission-safe related values.
- Preserve terminal field types so numeric, temporal, choice, and empty/not-empty filters behave like root fields.
- Resolve terminal lookup display labels through the existing permission-aware lookup resolver.
- Treat missing, deleted, inaccessible, or version-missing related values as empty without revealing why.
- Keep relationship traversal in the report module and expose no generic graph or relationship mutation API.
- Add no database migration; report definitions remain JSONB.

## Acceptance Criteria

- [ ] Existing list-report configs execute unchanged.
- [ ] One-hop related columns can be discovered, saved, run, sorted, searched, printed, and exported.
- [ ] Saved and runtime related filters use terminal field types and permission-safe values.
- [ ] Invalid, unknown, non-lookup, cyclic, and deeper paths are rejected with field-specific validation errors.
- [ ] Hidden root lookup or target fields never appear in the catalog, execution, print, or CSV output.
- [ ] Target form and record scopes are enforced for every related value without leaking raw IDs.
- [ ] Archived target forms retain readable existing relationships; deleted/missing/inaccessible targets resolve empty.
- [ ] Builder and viewer handle related field labels and types without a separate report builder.
- [ ] API/architecture documentation and backend/frontend tests are complete.
- [ ] Backend harness/build, frontend tests/build, PostgreSQL/API acceptance, and `git diff --check` pass.

## Out of Scope

- More than one lookup hop, reverse/one-to-many traversal, aggregates, joins across non-lookup fields, or cross-workspace relationships.
- Generic SQL expressions, formulas, arbitrary code, graph APIs, or relationship mutation.
- Related-record panels and operational row actions; those are later V10 tasks.
- A database migration or rewriting existing report configs.

## Tests

- Add backend catalog/path validation and typed related execution tests.
- Add frontend related field builder/API/viewer compatibility tests.
- Exercise authenticated create/run/filter/sort/export behavior against clean PostgreSQL, including archived and permission-restricted targets.

## Migration Notes

- No migration. One-hop dotted keys are stored in the existing report `config_json` JSONB document.
- Root field IDs remain unchanged and existing schema-version-1 configs deserialize without changes.

## Notes

- A key such as `parent_order.order_number` means traverse the root `parent_order` record lookup and read `order_number` from the permitted target record.
- Reverse related-record traversal belongs to task 006.
