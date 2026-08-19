# Task: Typed Operational Report and Row Actions

## Goal

Replace the report viewer's hardcoded controls with saved, ordered, permission-aware typed actions that reuse the platform's existing record, report, print, export, and audit boundaries.

## Context

Read `AGENTS.md`, `docs/MASTER_PRD_FOR_AI.md`, `docs/ARCHITECTURE.md`, `docs/API_SPEC.md`, `docs/PERMISSIONS.md`, `docs/DATA_MODEL.md`, `docs/CREATOR_APP_SUPPORT_ROADMAP.md`, `docs/V10_START_HERE.md`, `tasks/v10/README.md`, and this task file.

The report viewer currently renders New record, Print, Export CSV, View, Edit, and Delete controls directly in React. Their destination APIs enforce permissions, but the saved report definition cannot select or order them and the report execution response does not describe their effective availability. V10 task 007 turns those existing operations into a bounded typed contract without adding arbitrary scripts or a generic action executor.

## Requirements

- Extend list-report config with separate ordered `reportActions` and `rowActions` collections stored in the existing report config JSONB; do not add a database migration.
- Support only these report action types in this task: `create_record`, `print_report`, and `export_csv`.
- Support only these row action types in this task: `view_record`, `edit_record`, and `delete_record`.
- Give every action a stable config ID, supported type, bounded plain-text label, and enabled state. Allow confirmation text only for `delete_record` and keep it bounded plain text.
- Validate action IDs, types, labels, placement, uniqueness, confirmation metadata, and collection bounds on the backend. Reject unknown properties that could be interpreted as URLs, scripts, commands, templates, expressions, or request payloads.
- Limit each collection to one action of each supported type and at most eight definitions so later compatible types can be added without making the viewer unbounded.
- Preserve action order from the saved definition. Disabled actions remain editable in the builder but are absent from viewer execution projections.
- Normalize legacy list-report configs that have no action collections to the current safe behavior: create/print/export at report level and view/edit/delete at row level. Preserve the existing `rowOpenAction` contract independently for whole-row navigation.
- Add report-action definitions and effective availability to the report execution response. Add effective row-action definitions to each returned row because record scopes and workspace policies can differ by record.
- Project only enabled actions that the current caller may attempt in the current context. Do not return denial reasons, hidden actions, custom URLs, or raw permission policy details.
- Evaluate report actions through the existing source-form and saved-report permission rules: `create_record` requires form submit access, `print_report` requires the report view/run boundary, and `export_csv` requires form export plus saved-report export access.
- Evaluate row actions through the existing record authorization boundary for the concrete record and action: view, edit, or delete. Continue applying workspace access policies and scoped form permissions.
- Compute row availability in bounded batches for the returned page. Do not add one permission/database query per row or per action.
- Keep the destination operations authoritative. The frontend may hide unavailable actions, but record creation, report execution/export, record detail/edit, and record deletion endpoints must reauthorize every request.
- Reuse the existing record deletion endpoint and its audit, trigger/outbox, relationship, concurrency, and soft-delete behavior. Do not add a generic report-action execution endpoint.
- Reuse existing report print and CSV export behavior. A typed action selects an existing platform operation; it does not copy print/export implementation into the report viewer.
- Add report-builder controls for enabling, disabling, relabeling, and reordering the supported actions. Use an explicit catalog; do not provide free-form action type entry or JSON/script editing.
- Render report actions in an accessible responsive action menu or button group and row actions in the existing row menu. Preserve configured order, loading/disabled feedback, destructive confirmation, and keyboard operation.
- Remove the report viewer's unconditional hardcoded action rendering. Empty authorized action lists must render no empty menu or misleading disabled controls.
- After a successful row deletion, refresh the current page and move to the previous page when the deleted row was the last row on a non-first page.
- Keep actions out of browser and template print output.
- Add audit coverage only where the reused destination operation already requires it. Merely rendering or navigating through an action does not create a new audit event.

## Proposed Contract

Saved list-report config adds:

```json
{
  "reportActions": [
    { "id": "new", "type": "create_record", "label": "New record", "enabled": true },
    { "id": "print", "type": "print_report", "label": "Print", "enabled": true },
    { "id": "export", "type": "export_csv", "label": "Export CSV", "enabled": true }
  ],
  "rowActions": [
    { "id": "view", "type": "view_record", "label": "View", "enabled": true },
    { "id": "edit", "type": "edit_record", "label": "Edit", "enabled": true },
    {
      "id": "delete",
      "type": "delete_record",
      "label": "Delete",
      "enabled": true,
      "confirmation": "Delete this record?"
    }
  ]
}
```

The exact DTO names may follow existing report-module conventions. The execution response returns safe resolved action descriptors containing only the config ID, type, and label, plus delete confirmation when applicable. Report actions are returned once; row actions are returned per row. Routes are derived by trusted frontend helpers from the typed action and response IDs rather than persisted as config URLs.

## Acceptance Criteria

- [ ] Saved reports persist validated, ordered, enabled report and row action definitions without a schema migration.
- [ ] Legacy reports receive deterministic compatibility defaults and preserve existing whole-row navigation behavior.
- [ ] Unknown, duplicate, misplaced, excessive, or script-like action configuration is rejected by backend validation.
- [ ] Report execution returns only enabled actions the caller is authorized to use, with per-record row availability and no denial-policy details.
- [ ] Permission projection is bounded and avoids per-row/per-action database queries.
- [ ] The builder edits action enablement, labels, and order from a fixed supported catalog.
- [ ] The viewer renders the projected order, hides empty menus, supports keyboard use, confirms deletion, and refreshes pagination after deletion.
- [ ] Every destination endpoint independently reauthorizes the operation; direct API calls cannot bypass form, report, record-scope, or workspace-policy checks.
- [ ] Existing record deletion, CSV export, print, audit, trigger/outbox, relationship, and concurrency behavior remains authoritative and is not duplicated.
- [ ] Action controls are absent from browser and template print output.
- [ ] API, architecture, data-model, permission, roadmap, and V10 documentation plus backend/frontend tests are complete.
- [ ] Backend harness/build, frontend tests/build, authenticated PostgreSQL/API acceptance, and `git diff --check` pass.

## Out of Scope

- Arbitrary JavaScript, expressions, URLs, HTTP requests, commands, plugins, or a generic action execution engine.
- Duplicate-record semantics, record import, bulk selection/actions, inline editing, status/assignment editors, workflow transitions, trigger execution, retries, connector calls, or document generation.
- Row-level direct print actions or print-template selection; users can open an authorized record and use the existing record print workspace.
- Quick-view/detail-view layout builders, conditional action expressions, custom icons/colors, action groups, keyboard shortcuts, or mobile swipe gestures.
- New permission names, policy types, audit tables, action tables, background jobs, or database migrations.
- Actions on related-record panels; task 006 remains read-only.

## Tests

- Add backend validation and normalization tests for supported defaults, order, labels, enabled state, confirmation metadata, bounds, duplicates, placement, and hostile/unknown properties.
- Add backend execution tests for report-level create/print/export projection and per-row view/edit/delete projection under all/own/department/managed-department/group/assigned scopes and workspace-policy denials.
- Add query-shape or command-count coverage proving action projection does not perform per-row/per-action database queries.
- Add authorization tests proving unavailable actions are omitted and direct destination calls remain forbidden.
- Add regression coverage for legacy configs, `rowOpenAction`, hidden fields, nested report sources, soft-deleted records, deletion refresh behavior, CSV export, and print exclusion.
- Add frontend builder/API/viewer tests for fixed action catalogs, reorder/enable/label editing, ordered rendering, empty menus, confirmation, failure recovery, keyboard labels, and page fallback after deletion.
- Exercise authenticated PostgreSQL/API acceptance with roles that have mixed report, form-action, record-scope, and workspace-policy permissions.

## Migration Notes

- No migration is planned. Action definitions live in the existing report config JSONB.
- Missing action collections normalize to compatibility defaults when read or executed; do not rewrite saved rows merely to add defaults.
- Persisted explicit empty collections mean no actions, which is distinct from a missing legacy collection.
- If bounded permission projection cannot be implemented using existing permission/policy services without N+1 queries, stop and review the service/query change before widening the task.

## Review Decisions Proposed

- This task formalizes the six safe operations already exposed by the report viewer; it does not introduce a generic action engine.
- The backend projects effective availability while every destination endpoint remains authoritative.
- Action routes are derived from trusted typed definitions and IDs, never stored as user-authored URLs.
- Duplicate, import, bulk, direct row printing, workflows, retries, and integrations require separate semantics and remain later tasks.
- Missing collections provide legacy defaults; explicit empty collections intentionally remove all actions.
