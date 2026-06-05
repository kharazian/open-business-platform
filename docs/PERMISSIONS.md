# Permission Model

Status: V5 workflow transition execution extends the implemented V3/V4 permission model with workflow management, workflow action contract constants, record workflow execution through scoped record status access, and assigned-user approval inbox authorization. Local users, roles, groups, departments, department managers, per-form scoped record access, report access, action access, trigger management through form manage access, workflow management through form manage access, record workflow start/direct transition execution, current-user workflow approvals, and basic field-level hidden/read-only rules are implemented.

## Goals

Permissions must control who can:

- Submit forms
- View records
- Edit records
- Delete records
- Print records
- Export reports
- Manage forms
- Manage reports
- Manage permissions
- Approve assigned workflow steps

## Important Rule

Permission must be enforced by the backend.

Frontend permission checks are useful for hiding buttons, but they are not security.

## V1 Roles

- Admin
- Builder
- User
- Viewer

## V1 Permissions

Admin:

- Manage all forms
- View all records
- Edit all records
- Delete all records
- Manage users/access

Builder:

- Create forms
- Edit own forms or assigned forms
- View records for forms they manage

User:

- Submit forms
- View own records if allowed

Viewer:

- View records/reports if granted

## Implemented V1 Permission Keys

The bootstrap admin and users with the `Admin` role receive all built-in permissions through the current permission service.

Menu visibility:

- `menu.dashboard`
- `menu.forms`
- `menu.reports`
- `menu.users_access`
- `menu.settings`
- `menu.profile`

Platform actions:

- `users.manage`
- `roles.manage`
- `forms.create`
- `forms.manage_all`
- `reports.manage`
- `workflows.manage`

Per-form actions:

- `submit`
- `view`
- `edit`
- `delete`
- `manage`

The frontend filters navigation from the signed-in user's effective permissions. Backend APIs still enforce the actual permission checks.

V2 list report definition endpoints require `menu.reports` plus form view access for listing, and `reports.manage` plus form manage access for creation. V3 report run/export endpoints also use report-level view/export/manage grants when explicit report permissions exist. Report runs filter rows through scoped form `view` access; CSV exports require and filter rows through scoped form `export` access.

V4 trigger management endpoints require form `manage` access or `forms.manage_all`. V5 workflow definition management endpoints use the same form `manage` or `forms.manage_all` backend check for form-scoped workflow authoring. `workflows.manage` is available as a built-in platform permission constant for future workflow-wide administration surfaces.

## Implemented V3 Record Scopes

Per-form role grants now store an action and scope.

Actions:

- `submit`
- `view`
- `edit`
- `delete`
- `print`
- `export`
- `assign`
- `change_status`
- `manage`

Scopes:

- `all`
- `own`
- `department`
- `managed_department`
- `group`
- `assigned`

Multiple role grants combine with OR semantics. Per-form `manage` grants imply the other form actions but still use their configured record scope. `forms.manage_all`, Admin, and the bootstrap admin retain all-access behavior.

## Implemented V3 Field Rules

Role field permissions support:

- `hidden`: values are omitted from record, report, CSV, chart, and dashboard table responses.
- `read_only`: values are returned but backend record edits cannot change them.

## Permission Actions

Use consistent action names:

- create
- view
- edit
- delete
- submit
- print
- export
- approve
- assign
- comment
- change_status
- manage_form
- manage_report
- manage_permissions
- manage_workflow

## Permission Subjects

A permission subject can be:

- User
- Role
- Group
- Department
- Creator
- Owner
- Manager
- Everyone authenticated

## Permission Resources

Resources can be:

- Application
- Form
- Report
- Record
- Field
- Trigger
- Workflow

## Implemented V5 Workflow Permission Contracts

Workflow permission constants currently include:

- `view`
- `manage`
- `approve`

The backend workflow definition APIs validate management through form `manage` access. Record workflow state reads require record `view` access. Starting a workflow and requesting or executing a transition require record `change_status`, form `manage`, or `forms.manage_all` access with the same scoped record rules used by status changes. Current-user approval inbox APIs are scoped to tasks assigned to the authenticated persisted user; approving or rejecting a task is rejected when the task belongs to another user.

V5 transition action execution runs only after an authorized direct transition or assigned approval response. Supported workflow transition actions are `write_audit_entry`, `send_email`, `send_notification`, `assign_record`, `update_field`, and `create_record`; `change_status` is rejected so workflow state and `records.status` stay aligned. Record-mutating workflow actions reuse the initiating actor for audit metadata and do not recursively dispatch trigger events.

Prepared V5 follow-up tasks should keep these rules:

- Trigger-to-workflow starts must run through a typed trigger action boundary and must audit the trigger/action source.
- The optional visual workflow builder must still use form `manage` or `forms.manage_all` access; graph UI visibility is not a backend authorization substitute.

## Record-Level Rules Later

Examples:

- User can view only own records.
- Department manager can view department records.
- HR can view all employee records.
- Finance can export invoice records.
- User cannot edit after status is Approved.

## Field-Level Rules Later

Examples:

- Hide salary field from normal users.
- Make approval fields read-only for submitters.
- Allow HR to edit sensitive employee fields.

## Permission Service

Backend should have a central permission service like:

```csharp
Task<bool> CanAsync(ClaimsPrincipal principal, string permission, CancellationToken cancellationToken);
Task<bool> CanAccessFormAsync(ClaimsPrincipal principal, Guid formId, string action, CancellationToken cancellationToken);
```

For record queries, prefer methods that return allowed filters:

```csharp
IQueryable<Record> ApplyRecordAccess(UserContext user, IQueryable<Record> query, string action);
```

This avoids fetching records first and filtering in memory.
