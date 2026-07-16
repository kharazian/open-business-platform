# V9 Task 001: Workspace And Tenant Foundation

## Status

Complete.

## Goal

Establish explicit tenant and workspace ownership without changing the current single-workspace user experience.

## Scope

- Add tenant and workspace domain entities and PostgreSQL tables.
- Seed stable platform-default tenant and workspace rows in the migration.
- Add direct workspace ownership to persisted business, permission, audit, automation, notification, and integration data.
- Backfill all existing workspace-owned rows to the platform-default workspace.
- Add a scoped backend workspace context that resolves to the platform-default workspace in this task.
- Apply workspace query filters and write guards centrally in the EF Core DbContext.
- Add an authenticated read-only endpoint for the current workspace context.
- Preserve existing local users, login, permissions, seed data, APIs, and frontend behavior.

## Out Of Scope

- Workspace memberships, invitations, switching, or active-workspace claims.
- Tenant/workspace administration UI or mutation APIs.
- SSO, custom domains, branding, retention, or advanced policy rules.
- Moving users or password credentials into a workspace; V9 task 002 will introduce membership.

## Ownership Rules

- A tenant owns one or more workspaces.
- Business and operational rows belong directly to one workspace.
- Local users remain platform identities and gain workspace membership in V9 task 002.
- New workspace-owned rows receive the active workspace ID automatically.
- Attempts to insert a row for another workspace or mutate an existing row's workspace are rejected.
- Existing rows are backfilled to the stable platform-default workspace.

## Acceptance Criteria

- [x] Tenant and workspace entities have documented identifiers, slugs, status, and audit metadata.
- [x] The migration creates and backfills a default tenant/workspace without data loss.
- [x] Workspace-owned entities have indexed, required `workspace_id` foreign keys.
- [x] EF Core reads are filtered by the active workspace.
- [x] EF Core writes assign and enforce the active workspace.
- [x] `/api/workspaces/current` returns only the authenticated request's resolved context.
- [x] Existing single-workspace development startup and APIs continue to work.
- [x] Backend harness, build, migration consistency check, frontend tests, and frontend build pass.
- [x] Data model, architecture, security, roadmap, and V9 handoff documentation are updated.
