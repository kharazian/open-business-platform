# Task: Permission-Aware Related-Record Workspace

## Goal

Turn record detail into a safer operational workspace by showing paginated one-to-many panels for records whose lookup fields point to the current record.

## Context

Read `AGENTS.md`, `docs/MASTER_PRD_FOR_AI.md`, `docs/ARCHITECTURE.md`, `docs/API_SPEC.md`, `docs/DATA_MODEL.md`, `docs/CREATOR_APP_SUPPORT_ROADMAP.md`, `docs/V10_START_HERE.md`, and `tasks/v10/README.md`.

V10 task 004 added canonical lookup edges while preserving legacy JSON-only lookup values. Task 005 added permission-safe forward traversal inside reports. Record detail still has no reverse, one-to-many view of operational records that refer to the selected record.

## Requirements

- Add read-only related-record discovery and row endpoints under `/api/records/{recordId}`; keep traversal inside the records module and expose no generic graph or relationship mutation API.
- Require view access and matching record scope for the selected target record before discovering or loading any related panel.
- Define a panel by source form plus source lookup field, and collapse matching definitions from multiple immutable source form versions into one stable panel.
- Discover definitions whose `recordLookup` target form matches the selected record's form, including historical source versions needed to represent existing relationships.
- Require source-form view access and a visible source lookup field before returning a panel. Return a non-disclosing not-found response when a requested panel is unknown, inaccessible, or hidden.
- Match rows against each source record's stored immutable form version so obsolete or changed lookup definitions do not reinterpret historical JSON.
- Use canonical `record_relationships` edges as the indexed path and include active legacy JSON-only relationships through a compatibility path; deduplicate records present in both paths.
- Apply the existing source-form record scopes and workspace access policies before counting or returning source records, and exclude soft-deleted source records.
- Preserve readable existing relationships when a source or target form is archived; archival alone must not remove a panel or row for a caller who retains view access.
- Return panel metadata separately from paginated rows so every panel can load, retry, and paginate independently. Bound panel discovery to at most 25 panels per page and rows to at most 50 per page.
- Order panels deterministically by source form name, lookup label, and stable IDs. Order rows by newest creation time and record ID unless a later task adds configurable related views.
- Build a deterministic preview-column catalog from the current source-form schema when available, falling back to the newest applicable relationship version. Preserve schema/layout field order, omit the backlink lookup and subtable fields, apply current hidden-field rules, and cap preview columns at five.
- Return display-ready, permission-safe cell values rather than an unrestricted source record payload. Resolve lookup labels, attachment names, addresses, and other formatted values through existing record formatting/resolution boundaries.
- Never return hidden values, the backlink's raw target UUID, inaccessible lookup target UUIDs, attachment content/storage metadata, or failure reasons that distinguish missing from inaccessible data.
- Return an accessible source record ID, status, and creation time for each row so the frontend can link to the existing record detail route without introducing inline actions.
- Add a read-only related-record section to record detail with a global empty state plus independent panel loading, empty, error, retry, and pagination states.
- Keep related panels out of edit forms, selected print-template output, and browser print output in this task.
- Refresh related panels after the record detail refreshes, without making a related-panel failure fail the primary record detail request.
- Add no related-record create/edit/delete/retry buttons, configurable panel builder, report actions, or client-authored scripts; typed operational actions belong to V10 task 007.

## Proposed API Contract

- `GET /api/records/{recordId}/related?page=1&pageSize=10` returns a paged list of authorized panel descriptors with `sourceFormId`, `sourceFormName`, `sourceFieldId`, `sourceFieldLabel`, preview `columns`, and permission-filtered `totalCount`.
- `GET /api/records/{recordId}/related/{sourceFormId}/{sourceFieldId}?page=1&pageSize=10` returns the selected authorized panel descriptor plus a page of rows.
- Each row contains only `recordId`, `status`, `createdAt`, and display-ready `cells` keyed by the returned preview-column IDs.
- Panel and row totals count the union of canonical and compatible legacy relationships after source record authorization and deduplication.

The exact DTO names may follow existing records-module conventions, but the endpoint separation, authorization behavior, bounds, and non-disclosing response shape are part of this task.

## Acceptance Criteria

- [ ] Record detail discovers reverse one-to-many panels for lookup fields that target the selected record's form.
- [ ] Canonical and legacy JSON-only relationships appear once, using each source record's immutable schema to validate the edge.
- [ ] Target record access, source form access, source record scopes, workspace policies, and hidden source fields are enforced before metadata, counts, or rows are returned.
- [ ] Hidden backlink/preview fields and inaccessible related lookup values never expose raw UUIDs or other fallback values.
- [ ] Archived forms preserve otherwise-authorized existing relationships, while deleted, missing, or inaccessible source records do not appear.
- [ ] Panel discovery and rows are deterministically ordered, bounded, and independently paginated.
- [ ] The record detail UI provides responsive loading, empty, error, retry, pagination, and record-navigation states without breaking its existing value, workflow, timeline, edit, or print behavior.
- [ ] Related panels remain read-only and add no task-007 operational actions or generic relationship API.
- [ ] API, architecture, data-model, roadmap, and V10 documentation plus backend/frontend tests are complete.
- [ ] Backend harness/build, frontend tests/build, authenticated PostgreSQL/API acceptance, and `git diff --check` pass.

## Out of Scope

- Inline related-record creation, editing, deletion, duplication, retry, workflow, trigger, print, export, or other operational actions.
- User-configurable related panel definitions, saved filters, custom sorting, search, aggregates, charts, or report embedding.
- Forward lookup detail cards, more than one lookup hop, many-to-many or multi-select relationships, polymorphic relationships, or cross-workspace traversal.
- A generic relationship query/mutation API, arbitrary SQL/expressions, client scripts, or user-authored code.
- Rewriting historical record JSON or mutating immutable form versions.

## Tests

- Add backend tests for panel discovery, canonical/legacy union and deduplication, immutable-version matching, deterministic columns/order, pagination bounds, and archived-form behavior.
- Add backend authorization tests for target record scope, source form access, source record scopes, workspace policy denials, hidden backlink fields, hidden preview fields, and non-disclosing invalid panel requests.
- Add response-projection tests proving lookup/file display values are safe and raw inaccessible IDs or protected metadata are absent.
- Add frontend API/helper/component coverage for independent panel loading, global/panel empty states, retry, pagination, navigation, edit/print exclusion, and primary-detail resilience.
- Exercise authenticated discovery and row paging against PostgreSQL with canonical, legacy-only, archived, hidden-field, and permission-restricted fixtures.

## Migration Notes

- No migration is planned. Canonical reverse queries use the V10 task 004 `record_relationships` indexes.
- Legacy JSON-only records remain readable through a bounded compatibility query and are not rewritten by a read request.
- If implementation evidence shows the compatibility path cannot be safely bounded with the existing indexes, stop and review a migration/backfill proposal rather than silently adding schema or mutating records.

## Review Decisions Captured

- Related data uses separate discovery and row endpoints rather than expanding the primary record-detail payload.
- Panels are derived from versioned lookup definitions; there is no new panel-definition builder in this task.
- Preview columns are deterministic and capped at five; configurable related views belong to a later task.
- The workspace is read-only. V10 task 007 owns typed report and row actions.
