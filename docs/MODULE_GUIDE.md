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

Future feature modules should live under `src/app/src/features`.

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
