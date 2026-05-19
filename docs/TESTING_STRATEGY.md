# Testing Strategy

## Current Status

The current skeleton has build validation, lightweight frontend logic tests, and a lightweight backend executable test harness.

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

The current frontend `npm test` command runs lightweight Node-based tests for shared TypeScript logic. The backend test harness runs focused assertions without external test dependencies. Add fuller frontend/backend test projects when product modules need component, unit, or integration coverage.

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

## Test Commands

Current commands:

Frontend:

```bash
cd src/app
npm test
```

Backend:

```bash
dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj
```
