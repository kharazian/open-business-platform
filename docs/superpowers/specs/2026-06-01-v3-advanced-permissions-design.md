# V3 Advanced Permissions Design

## Status

Approved for implementation on 2026-06-01.

V3 completes the roadmap slice for organization-aware access control. It builds on the existing V1/V2 foundation: users, roles, departments, per-form role permissions, record owner/department columns, report definitions, audit logs, and backend permission checks.

## Goals

- Add groups as first-class access subjects.
- Let admins manage department managers and user group membership.
- Enforce record access scopes on the backend for list, detail, edit, delete, report run, report export, charts, and dashboards.
- Add report-level permissions without breaking existing V2 reports.
- Add action-level permissions for sensitive record/report actions.
- Add basic field-level hidden/read-only rules.
- Keep permissions centralized so future triggers and workflows can reuse the same checks.

## Non-Goals

- No custom rule language or arbitrary ABAC expressions.
- No nested groups.
- No workspace/tenant ownership.
- No workflow approval permissions beyond the action keys needed by V3.
- No visual permission graph builder.
- No XYFlow.

## Permission Model

V3 keeps roles as the main grant container. Users gain effective access from all active roles plus their department and group memberships.

Record access uses scoped per-form actions. The action still says what the user can do: `view`, `edit`, `delete`, `print`, `export`, `assign`, or `change_status`. The scope says which records the action applies to:

- `all`: all non-deleted records for the form.
- `own`: records where the user is owner or creator.
- `department`: records whose department is one of the user's departments.
- `managed_department`: records whose department is managed by the user.
- `group`: records assigned to one of the user's groups.
- `assigned`: records assigned directly to the user.

Scopes from multiple roles combine with OR semantics. A user with `view/own` from one role and `view/department` from another can view records matching either scope. `manage` on a form implies all scoped form actions for that form. `forms.manage_all`, Admin, and the bootstrap admin continue to bypass scoped limits.

## Data Model

New tables:

- `groups`: `id`, `name`, `description`, `is_active`, `concurrency_stamp`, audit columns, optional JSON properties.
- `user_groups`: `user_id`, `group_id`.
- `role_report_permissions`: `id`, `role_id`, `report_id`, `action`.
- `role_field_permissions`: `id`, `role_id`, `form_id`, `field_id`, `access`.

Changed tables:

- `role_form_permissions`: add `scope`, default `all`.
- `records`: add `assigned_to_user_id`, `assigned_group_id`.

Indexes:

- `user_groups.user_id`, `user_groups.group_id`.
- unique `user_id + group_id`.
- `role_report_permissions.role_id`, `report_id`, unique `role_id + report_id + action`.
- `role_field_permissions.role_id`, `form_id`, unique `role_id + form_id + field_id`.
- `records.assigned_to_user_id`, `records.assigned_group_id`.

Report actions are `view`, `export`, and `manage`. Field access values are `hidden` and `read_only`. Hidden is stricter than read-only and wins if multiple roles disagree.

## Backend Components

`PermissionService` becomes the single place for access decisions:

- `GetEffectivePermissionsAsync(...)` keeps returning global/menu permissions.
- `CanAccessFormAsync(...)` keeps the current simple boolean behavior for existing callers.
- `GetAllowedRecordScopesAsync(...)` resolves scoped form grants for a user, form, and action.
- `ApplyRecordAccess(...)` applies scoped access to an `IQueryable<FormRecord>`.
- `CanAccessRecordAsync(...)` checks one record using the same scoped rules.
- `GetFieldAccessAsync(...)` returns hidden/read-only field IDs for a user and form.
- `CanAccessReportAsync(...)` checks report-level access.

Records:

- Record list uses `ApplyRecordAccess` before paging/searching.
- Record detail uses `CanAccessRecordAsync`.
- Record edit rejects changes to read-only or hidden fields.
- Record delete uses scoped delete access.
- Record assignment endpoint assigns a record to a user or group and requires scoped `assign`.
- Record status endpoint changes the record status and requires scoped `change_status`.

Reports/charts/dashboards:

- Report listing remains form-based, but report run requires report `view` when report-specific permissions exist.
- CSV export requires report `export` or report `manage` when report-specific permissions exist, and also requires the global or form-level export action.
- Report run/export source records are filtered through `ApplyRecordAccess`.
- Chart previews and dashboard widgets that query records use the same record filter.
- Hidden fields are removed from report columns, report cells, CSV output, chart table rows, and dashboard table widgets.

Identity:

- Users & Access gains group management endpoints and department management endpoints.
- User create/update accepts role IDs, department IDs, and group IDs.
- Role permission payloads include scoped form grants, report grants, and field grants.

## Frontend Components

Users & Access remains the V3 management workspace. It gains:

- Users tab: role, department, and group assignment.
- Roles tab: menu/platform permissions, scoped form permissions, report permissions, and field rules.
- Departments tab: department CRUD plus manager assignment.
- Groups tab: group CRUD plus membership visibility.

Controls stay dense and operational:

- Scope uses a select beside each form/action permission.
- Report permissions use a compact table grouped by form/report.
- Field rules use a form selector, field list, and hidden/read-only toggles.

The frontend can hide buttons and columns for UX, but backend responses remain authoritative. Hidden field values must not be present in API responses.

## Data Flow

1. Admin creates users, departments, groups, and roles.
2. Admin grants a role scoped form access, report access, action access, and optional field rules.
3. User signs in. The frontend receives global/menu permissions as before.
4. User opens records or reports.
5. Backend resolves the user's active roles, department IDs, managed department IDs, and group IDs.
6. Backend applies scoped filters before returning records or report rows.
7. Backend removes hidden fields and returns read-only field IDs with record detail responses for the UI.
8. Backend rejects edits that attempt to change hidden/read-only fields or records outside the user's scoped access.

## Error Handling

- `403` for failed action, record, report, or field authorization.
- `404` when a requested record/report/group/department does not exist.
- `400` for invalid permission payloads, invalid scopes, invalid report actions, invalid field access values, or assignment to missing inactive subjects.
- `409` for concurrency conflicts and stale schemas.

Hidden fields are omitted, not returned as `null`. This prevents clients from distinguishing hidden values from empty values.

## Audit

V3 continues using `audit_logs` for sensitive changes:

- `group_created`, `group_updated`, `group_disabled`
- `department_created`, `department_updated`, `department_disabled`
- `user_groups_changed`
- `role_permissions_changed`
- `record_assigned`
- `record_status_changed`
- `report_permissions_changed`
- `field_permissions_changed`

Existing record/report audit events continue to include the acting user when available.

## Backward Compatibility

Existing `role_form_permissions` rows migrate to scope `all`.

Existing reports remain accessible through the current form-based rules until explicit `role_report_permissions` rows are created for that report. Once a report has at least one report permission row, report-specific permissions apply.

Admin, bootstrap admin, and `forms.manage_all` retain all-access behavior.

## Testing

Backend harness coverage should include:

- own-record access allows owner/creator and denies other users.
- department scope allows records in the user's department.
- managed department scope allows department managers.
- group scope allows records assigned to the user's group.
- assigned scope allows records assigned directly to the user.
- report run/export only include records allowed by scoped access.
- hidden fields are absent from detail, report, CSV, chart table, and dashboard table responses.
- read-only fields cannot be changed through record edit.
- Admin/all-access still sees and edits records.

Frontend tests should cover:

- permission payload normalization.
- scoped form grant helpers.
- group/department user draft serialization.
- field rule helper behavior.

## Documentation And Tasks

Implementation should add or update:

- `tasks/v3/README.md`
- `tasks/v3/001-advanced-permission-foundation.md`
- `docs/PERMISSIONS.md`
- `docs/SECURITY_MODEL.md`
- `docs/DATA_MODEL.md`
- `docs/API_SPEC.md`
- `docs/ROADMAP.md`
- `docs/MASTER_PRD_FOR_AI.md`

V3 is complete when all roadmap bullets under Advanced Permissions are represented by implemented backend checks, management UI, documentation, and passing verification.
