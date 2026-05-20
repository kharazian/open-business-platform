# Permission Model

Status: V1 role-based access foundation is implemented for local users, roles, menu visibility, and per-form access. More advanced rule subjects such as departments, owners, groups, and field-level access remain future work.

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
- Approve workflow steps later

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

Per-form actions:

- `submit`
- `view`
- `edit`
- `delete`
- `manage`

The frontend filters navigation from the signed-in user's effective permissions. Backend APIs still enforce the actual permission checks.

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
