# Users, Roles, and Form Access Design

## Purpose

Build the first real users and access foundation for the platform. This slice replaces the sample users page with working local user management, role management, role-based menu visibility, and role-based per-form access.

The first version is intentionally role-based only. Per-user overrides, department rules, record ownership rules, field-level permissions, email reset links, and advanced allow/deny rule priority are later extensions.

## Scope

In scope:

- Local email/password users stored in PostgreSQL.
- Admin create/edit/disable users.
- Admin manual password reset by setting a new temporary password.
- Role create/edit/disable.
- Assign roles to users.
- Define role access to app menu items.
- Define role access to forms by action.
- Return effective permissions for the signed-in user.
- Filter frontend navigation from effective permissions.
- Enforce access checks on backend endpoints for users, roles, and forms.
- Audit sensitive actions where the existing audit foundation supports it.

Out of scope:

- Email invitation and reset-link flows.
- Per-user permission overrides.
- Department, owner, manager, and group-based access rules.
- Field-level permissions.
- Advanced custom condition builder.
- Workflow approval permissions.
- External identity providers.

## Backend Design

Authentication will support local user login against the `users` table. The current bootstrap admin remains a setup fallback so a fresh environment can still sign in before the first local admin exists. The bootstrap admin receives all built-in menu, platform, and form-management permissions in memory and does not need matching database role rows. Local credentials must store password hashes, never plaintext passwords.

The identity module will expose management APIs for users and roles. Endpoint handlers stay thin and delegate validation, password hashing, role assignment, permission assignment, and audit writing to services.

The permission model uses roles as the only subject type for this slice. A role can grant platform actions, menu visibility, and per-form actions.

Permission keys:

- Menu: `menu.dashboard`, `menu.forms`, `menu.reports`, `menu.users_access`, `menu.settings`, `menu.profile`
- Platform actions: `users.manage`, `roles.manage`, `forms.create`, `forms.manage_all`
- Form actions: `submit`, `view`, `edit`, `delete`, `manage`

Per-form permissions are stored as role-to-form action rows rather than opaque strings so the backend can query and validate them cleanly. Menu and platform permissions can use a role permission table keyed by action name because they are global.

The backend permission service exposes explicit checks:

```csharp
Task<bool> CanAsync(ClaimsPrincipal user, string permission, CancellationToken cancellationToken);
Task<bool> CanAccessFormAsync(ClaimsPrincipal user, Guid formId, string action, CancellationToken cancellationToken);
```

User and role management endpoints require `users.manage` or `roles.manage`. Form endpoints require global form permissions or the relevant per-form permission. The frontend may hide UI, but these checks are the source of truth.

The first local administrator can be created while signed in as the bootstrap admin. After that, normal local users with an admin role can manage access through the same APIs.

## Data Model

Add credential and permission storage:

- `users.password_hash`
- `users.password_updated_at`
- `role_permissions`
  - `role_id`
  - `permission`
- `role_form_permissions`
  - `role_id`
  - `form_id`
  - `action`

Keep existing `users`, `roles`, and `user_roles` tables. Existing departments remain available but are not required for this slice.

Indexes:

- `role_permissions.role_id`
- unique `role_permissions(role_id, permission)`
- `role_form_permissions.role_id`
- `role_form_permissions.form_id`
- unique `role_form_permissions(role_id, form_id, action)`

## API Design

Auth:

- `POST /api/auth/login`
- `GET /api/auth/me`
- `POST /api/auth/logout`

`/api/auth/me` returns user identity, roles, and effective permissions.

Users:

- `GET /api/users`
- `POST /api/users`
- `GET /api/users/{id}`
- `PUT /api/users/{id}`
- `POST /api/users/{id}/reset-password`

Roles:

- `GET /api/roles`
- `POST /api/roles`
- `GET /api/roles/{id}`
- `PUT /api/roles/{id}`
- `PUT /api/roles/{id}/permissions`

Forms access:

- Form APIs continue to live in the forms module.
- Form list and form detail responses should only include forms the user can view or manage.
- Create, edit, publish, delete, and later record actions must call the permission service.

## Frontend Design

Replace the starter Users page with a real Users & Access module. The module contains tabs or sibling pages for:

- Users
- Roles
- Role permissions

Users view:

- Searchable table with name, email, roles, status, created date.
- Create user modal/page with name, email, password, active state, roles.
- Edit user modal/page with name, active state, roles.
- Reset password action with a manual new-password field.

Roles view:

- Role table with name, description, active state, user count.
- Create/edit role.

Role permissions view:

- Menu visibility checklist.
- Platform permission checklist.
- Form access matrix where each form row exposes `submit`, `view`, `edit`, `delete`, and `manage`.

Navigation filtering:

- Frontend module navigation keeps permission metadata.
- `AppLayout` filters navigation using the signed-in user's effective permissions.
- Routes still render through `RequireAuth`, and sensitive pages also check the relevant permission so hidden routes are not casually accessible from the address bar.

## Error Handling

Backend validation returns clear `400` responses for invalid names, emails, duplicate emails, missing roles, unknown forms, invalid permission keys, and weak or empty passwords.

Authorization failures return:

- `401` for anonymous users.
- `403` for authenticated users without the required permission.

Concurrency conflicts use existing concurrency stamps where applicable.

Password reset does not reveal old passwords and does not return password hashes.

## Audit

Audit these actions:

- User created.
- User updated.
- User disabled or re-enabled.
- User password manually reset.
- Role created.
- Role updated.
- Role permissions changed.

Audit entries should avoid storing plaintext passwords or password hashes.

## Testing

Backend tests:

- Password hashing verifies a correct password and rejects an incorrect password.
- Local user login succeeds for active users and fails for inactive users.
- Bootstrap admin fallback still works.
- Permission service returns role permissions and per-form access correctly.
- User and role management endpoints require management permissions where practical in the existing lightweight harness.
- EF model maps new permission tables and indexes.

Frontend tests:

- User and role access types compile under strict TypeScript.
- Navigation filtering includes permitted items and excludes missing permissions.
- Auth client accepts effective permissions from `/api/auth/me`.

## Migration Notes

This design changes the database schema, so an EF Core migration must document the new columns and tables. Existing environments should retain the bootstrap admin fallback so setup does not lock out administrators during the transition.

## Future Extensions

- Email invitation and password reset links through the notification engine.
- Per-user overrides.
- Department and ownership rules.
- Field-level hidden/read-only permissions.
- Report export and print permissions.
- Workflow approval permissions.
- A richer permission rule engine with explicit allow/deny priority.
