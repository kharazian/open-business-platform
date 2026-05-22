# Testing Strategy

## Current Status

The current repository has build validation, lightweight frontend logic/API tests, a lightweight backend executable test harness, and manual API smoke coverage for the finalized V1 baseline.

The frontend package declares Node.js `>=20.19.0`. `npm run build` relies on Vite and should be run with that Node version or newer.

Current validation commands:

```bash
cd src/app
npm test
npm run build
```

```bash
dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj
cd src/api
dotnet build
```

The current frontend `npm test` command runs Vitest-based tests for shared TypeScript logic, API clients, module navigation filtering, auth parsing, users/forms types, form-builder helpers, and small shared UI helper coverage. The backend test harness runs focused assertions, including EF Core model metadata checks for UUID IDs, entity inheritance, JSONB mappings, role/form permission mappings, password hashing, permission constants, form DTOs, and repository primitives, without external test dependencies. Add fuller frontend/backend test projects when product modules need component, unit, or integration coverage.

The V1 finalization smoke path should cover health, demo login, current session, forms list, published form rendering, records list, record detail, unauthenticated rejection, and viewer permission denials.

## Backend Tests

Recommended test categories:

### Unit Tests

Use for:

- Form schema validation
- Record value validation
- Permission evaluation
- Trigger condition evaluation
- Report filter building

### Integration Tests

Use for:

- API endpoints
- Database persistence
- Permission-protected record queries
- Form publish and record submit flow

### Recommended Backend Tools

- xUnit or NUnit
- FluentAssertions optional
- Testcontainers for PostgreSQL later

## Frontend Tests

Recommended test categories:

### Unit Tests

Use for:

- Schema helper functions
- Layout mapping functions
- Field rendering logic

### Component Tests

Use for:

- FormRenderer
- FieldSettingsPanel
- RecordList
- ReportViewer later

### E2E Tests Later

Use for:

- Create form
- Publish form
- Submit record
- View record
- Print record

## V1 Priority Tests

- Form schema validation
- Backend record creation
- Backend permission checks
- Form version immutability
- Basic FormRenderer component

Status: covered by the current lightweight test suite and V1 smoke checks at the foundation level. Broader integration and component test suites should be added as later modules increase risk.

## Test Commands

Current commands:

Frontend:

```bash
cd src/app
npm test
```

Frontend tests are discovered by Vitest from `*.test.*` files instead of being listed manually in `package.json`.

Backend:

```bash
dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj
dotnet ef migrations has-pending-model-changes --project src/api/OpenBusinessPlatform.Api.csproj --startup-project src/api/OpenBusinessPlatform.Api.csproj
```
