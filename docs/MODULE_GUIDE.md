# Module Guide

This guide explains how to add product modules as the platform grows.

## Module Principle

A module should own one product capability and keep its frontend, backend, and database responsibilities clear.

Examples:

- Forms
- Records
- Reports
- Permissions
- Triggers
- Workflows
- Printing
- Audit
- Notifications

## Frontend Module Shape

Product domain code should live under `src/app/src/features`.

Example:

```text
src/app/src/features/forms/
  api/
  components/
  hooks/
  pages/
  types/
  utils/
```

Current app route/navigation modules live under `src/app/src/modules`. Each module should export a `PlatformModule` with explicit routes, navigation items, permissions, and ownership metadata when applicable.

Example:

```text
src/app/src/modules/dashboard/module.tsx
src/app/src/modules/users/module.tsx
```

`src/app/src/modules/index.ts` registers modules, and `src/app/src/platform/moduleRegistry.ts` derives sorted routes and navigation from that registry.

Use shared UI and layout components from:

```text
src/app/src/components/ui
src/app/src/components/layout
```

Do not duplicate shared UI inside feature folders or `/theme`.

## Backend Module Shape

Backend modules should live under `src/api/Modules`.

Example:

```text
src/api/Modules/Forms/
  FormsEndpoints.cs
  FormsService.cs
  FormsContracts.cs
```

Current backend modules use minimal APIs. A module that maps endpoints should implement `IPlatformApiModule` from `src/api/Platform`, and `Program.cs` discovers those modules through `app.MapPlatformApiModules()`.

Example:

```text
src/api/Modules/Dashboard/
  DashboardModule.cs
  DashboardEndpoints.cs
```

As modules become larger, split them into focused files. Do not create empty layers just to match a pattern.

## Module Rules

- Keep module APIs explicit.
- Keep endpoint handlers thin.
- Keep business logic out of UI components and endpoint handlers.
- Validate input on the backend.
- Enforce permissions on the backend.
- Write audit logs for sensitive actions.
- Add tests for important logic.
- Update docs when module contracts, schemas, or commands change.

## Cross-Module Communication

Early versions can call application services directly inside the modular monolith.

Later versions may introduce internal events for actions such as:

- Form published
- Record created
- Record updated
- Record deleted
- Permission changed
- Trigger executed

Do not add a message bus until there is a real need.
