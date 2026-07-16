# V9 Task 002: Workspace Membership And User Context

## Status

Complete.

## Goal

Resolve every authenticated request through an active workspace membership and keep roles, permissions, users, and integration identities scoped to that workspace.

## Scope

- Add workspace membership persistence with member role, status, default selection, lifecycle timestamps, and audit metadata.
- Backfill existing users into the platform-default workspace without changing current access.
- Resolve the login workspace from an active membership and store it in the signed authentication cookie.
- Validate cookie-authenticated membership on every request so suspension takes effect without waiting for cookie expiry.
- Recalculate role claims and effective permissions inside the selected workspace.
- List available workspaces and support membership-checked workspace switching.
- Scope user/directory management to memberships in the active workspace.
- Assign new users an active membership in the workspace where they are created.
- Add the owning workspace claim to integration API-key principals.
- Define and test invited, active, and suspended membership transitions.

## Out Of Scope

- Invitation email/token delivery and anonymous invitation acceptance.
- Tenant/workspace creation or mutation APIs and workspace switching UI.
- SSO, custom domains, workspace branding, and advanced enterprise policy.
- Cross-workspace platform administration beyond the bootstrap administrator's default-workspace compatibility access.

## Security Rules

- Workspace context comes only from a signed cookie claim, an authenticated integration identity, or the trusted system default.
- Local users must have an active membership in the selected active workspace and tenant.
- Role claims are loaded for the selected workspace and are replaced on every workspace switch.
- A caller cannot switch to an invited, suspended, inactive, or unrelated workspace.
- Workspace membership lifecycle mutations require `users.manage` and write audit entries.
- Identity lookup during login may be global; authenticated user/directory results must be membership-scoped.

## Acceptance Criteria

- [x] Existing users receive compatible default-workspace memberships through migration backfill.
- [x] New users receive active membership in the current workspace.
- [x] Login and `/api/auth/me` expose the signed active workspace identifier.
- [x] Suspended or missing membership blocks cookie-authenticated requests.
- [x] Workspace switching verifies active membership and refreshes workspace-specific role claims.
- [x] Available-workspace and membership-management endpoints are authenticated and backend-authorized.
- [x] User and directory queries do not expose users outside the active workspace.
- [x] Integration API-key principals carry their persisted workspace identifier.
- [x] Backend harness, build, migration consistency, frontend tests, and frontend build pass.
- [x] API, data model, security, architecture, roadmap, and V9 handoff docs are updated.
