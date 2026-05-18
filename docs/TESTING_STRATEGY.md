# Testing Strategy

## Current Status

The current skeleton has build validation and lightweight frontend logic tests, but no dedicated frontend/backend test projects yet.

The frontend package declares Node.js `>=20.19.0`. `npm run build` relies on Vite and should be run with that Node version or newer.

Current validation commands:

```bash
cd src/app
npm test
npm run build
```

```bash
cd src/api
dotnet build
```

`npx tsc --noEmit` can be used as a frontend type-check fallback when the local Node version can run TypeScript but is too old for Vite.

The current frontend `npm test` command runs lightweight Node-based tests for shared TypeScript logic. Add fuller frontend/backend test projects when product modules need component, unit, or integration coverage.

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

Current frontend command:

Frontend:

```bash
cd src/app
npm test
```

Future backend command once test projects are introduced:

```bash
dotnet test
```
