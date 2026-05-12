# Testing Strategy

## Current Status

The current skeleton has build validation but no dedicated test projects yet.

Current validation commands:

```bash
cd src/app
npm run build
```

```bash
cd src/api
dotnet build
```

Add formal test commands when frontend/backend test projects are introduced.

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

Future commands:

Frontend:

```bash
npm run test
```

Backend:

```bash
dotnet test
```
