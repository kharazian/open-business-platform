# Users Roles Access Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build local users, roles, manual password reset, role-based menu visibility, and role-based per-form access.

**Architecture:** Extend the existing identity module with local password auth, management endpoints, and a central permission service. Add focused EF entities for role permissions and role form permissions, then let the frontend consume effective permissions from `/api/auth/me` to filter navigation and render a Users & Access workspace.

**Tech Stack:** ASP.NET Core minimal APIs, EF Core/Npgsql, PostgreSQL, React, React Router, TypeScript, Vite, Tailwind CSS, lucide-react.

---

## File Structure

Backend:

- Modify `src/api/Domain/Entities/User.cs` for password hash metadata.
- Modify `src/api/Domain/Entities/Role.cs` for permission navigation.
- Create `src/api/Domain/Entities/RolePermission.cs` for global role permission rows.
- Create `src/api/Domain/Entities/RoleFormPermission.cs` for per-form action rows.
- Modify `src/api/Infrastructure/Persistence/OpenBusinessPlatformDbContext.cs` to map new columns and tables.
- Add an EF migration in `src/api/Infrastructure/Persistence/Migrations`.
- Create `src/api/Modules/Identity/PlatformPermissions.cs` for permission keys and form actions.
- Create `src/api/Modules/Identity/LocalPasswordHasher.cs` for PBKDF2 password hashing.
- Create `src/api/Modules/Identity/PermissionService.cs` for effective permission checks.
- Create `src/api/Modules/Identity/IdentityManagementService.cs` for users, roles, and permission changes.
- Modify `src/api/Modules/Identity/IdentityContracts.cs` and `IdentityEntityContracts.cs` for effective permissions and management DTOs.
- Modify `src/api/Modules/Identity/IdentityEndpoints.cs` for local login and management endpoints.
- Modify `src/api/Program.cs` to register new services.
- Create `src/api/Modules/Forms/FormsModule.cs`, `FormsEndpoints.cs`, and `FormAccessContracts.cs` for minimal form options.
- Modify `src/api.Tests/Program.cs` with failing tests before implementation.

Frontend:

- Modify `src/app/src/features/auth/types.ts` and `authClient.ts` for effective permissions.
- Add tests to `src/app/src/features/auth/authClient.test.mjs`.
- Modify `src/app/src/platform/moduleRegistry.ts` and its tests for permission-aware navigation.
- Add `src/app/src/features/users/api.ts` for Users & Access API calls.
- Extend `src/app/src/features/users/types.ts` and `userTypes.test.mjs`.
- Add `src/app/src/features/users/pages/UsersAccessPage.tsx`.
- Modify `src/app/src/modules/users/module.tsx` and other module definitions to add menu permissions.
- Modify `src/app/src/layouts/AppLayout.tsx` and `src/app/src/App.tsx` to filter navigation and guard routes.
- Modify `src/app/src/pages/Users.tsx` to delegate to the new feature page or replace it.

## Task 1: Backend Permission Model and Hashing

- [ ] Write failing backend harness assertions for `LocalPasswordHasher`, permission constants, EF mappings, and permission service behavior in `src/api.Tests/Program.cs`.
- [ ] Run `dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj`; expect compile failures for missing types.
- [ ] Add `User.PasswordHash`, `User.PasswordUpdatedAt`, `RolePermission`, `RoleFormPermission`, and DbContext mappings.
- [ ] Add `PlatformPermissions`, `LocalPasswordHasher`, and `PermissionService`.
- [ ] Run the backend harness; expect the new assertions to pass or reveal the next missing piece.

## Task 2: Backend Users and Roles APIs

- [ ] Write failing backend harness assertions for DTOs: reset-password request, role permissions request, effective permissions in auth response.
- [ ] Add management DTOs to `IdentityEntityContracts.cs` and auth response permissions to `IdentityContracts.cs`.
- [ ] Implement `IdentityManagementService` with list/create/update/reset-password and role permissions operations.
- [ ] Update `IdentityEndpoints.cs` to map `/api/users`, `/api/roles`, and `/api/roles/{id}/permissions`.
- [ ] Update login to try local active users before bootstrap fallback.
- [ ] Register services in `Program.cs`.
- [ ] Run the backend harness and `dotnet build src/api/OpenBusinessPlatform.Api.csproj`.

## Task 3: Minimal Forms Access API

- [ ] Write failing backend harness assertions for `FormAccessOptionDto`.
- [ ] Add `FormsModule`, `FormsEndpoints`, and `FormAccessContracts`.
- [ ] Implement `GET /api/forms/access-options` for users with form management permissions.
- [ ] Run the backend harness and API build.

## Task 4: Frontend Auth and Navigation Permissions

- [ ] Write failing frontend tests showing auth parsing accepts `permissions` and navigation filtering hides missing permissions.
- [ ] Extend auth types/client parsing.
- [ ] Extend `NavigationItem` with `permission` and add `filterNavigationByPermissions`.
- [ ] Add permission metadata to modules.
- [ ] Filter navigation in `AppLayout` and guard route elements in `App.tsx`.
- [ ] Run `npm test` in `src/app`.

## Task 5: Frontend Users & Access Workspace

- [ ] Write failing frontend type/API tests for users, roles, role permission payloads, and form access actions.
- [ ] Implement `features/users/api.ts`.
- [ ] Implement `UsersAccessPage` with Users, Roles, and Role permissions tabs.
- [ ] Wire `modules/users/module.tsx` to the new page and menu permission.
- [ ] Run `npm test` and `npm run build` in `src/app`.

## Task 6: Migration, Docs, and Final Verification

- [ ] Add EF migration for password and role permission schema.
- [ ] Update `docs/API_SPEC.md`, `docs/PERMISSIONS.md`, and `docs/DATA_MODEL.md` for new endpoints and schema.
- [ ] Run backend harness.
- [ ] Run backend build.
- [ ] Run frontend tests.
- [ ] Run frontend build.
- [ ] Review `git diff` and summarize changed files, tests, risks, and follow-up work.
