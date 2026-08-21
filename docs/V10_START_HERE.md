# V10 Start Here

This packet is the handoff for V10: Operational App Modeling.

## Current State

- V1 through V9 are complete for their current task lists.
- V9 is accepted as the enterprise ownership, identity, policy, and administration checkpoint.
- V10 tasks 001 through 010 are complete and accepted in `docs/V10_FINALIZATION.md`.
- Existing form schemas support currency, percent, rating, URL, time, datetime, user/department pickers, structured addresses, autonumbers, protected file attachments, record lookups, and child-record subtables.
- No subsequent version is currently scoped. New product work requires a separately reviewed plan.

## Read In This Order

1. `AGENTS.md`
2. `docs/MASTER_PRD_FOR_AI.md`
3. `docs/ROADMAP.md`
4. `docs/CREATOR_APP_SUPPORT_ROADMAP.md`
5. `docs/V9_FINALIZATION.md`
6. `docs/V10_FINALIZATION.md`
7. `docs/API_SPEC.md`
8. `docs/DATA_MODEL.md`
9. `tasks/v10/README.md`
10. The selected `tasks/v10/*.md` task file.

## Direction

V10 made operational forms and records more expressive while preserving the existing schema/version, permission, report, workflow, and integration boundaries.

Completed sequence:

1. Structured address fields.
2. Backend-generated autonumber fields (`tasks/v10/002-backend-generated-autonumber-fields.md`).
3. Real file attachment storage and protected access (`tasks/v10/003-protected-file-attachments.md`).
4. Stronger lookup relationship integrity (`tasks/v10/004-lookup-relationship-integrity.md`).
5. Nested relationship columns and filters in reports (`tasks/v10/005-nested-report-relationships.md`).
6. Permission-aware related-record panels in record detail (`tasks/v10/006-related-record-workspace.md`; complete).
7. Typed operational report and row actions (`tasks/v10/007-typed-operational-report-actions.md`; complete).
8. File-processing and scheduled job definitions (`tasks/v10/008-bounded-processing-jobs-and-schedules.md`; complete).
9. Operational logs and failure notifications (`tasks/v10/009-processing-operations-and-failure-notifications.md`; complete).
10. Analysis-only Creator-style export assistant (`tasks/v10/010-analysis-only-creator-export-assistant.md`; complete).

## Scope Boundaries

Do:

- Evolve frontend and backend schema contracts together.
- Keep every published form version immutable.
- Validate record values against the stored form version.
- Enforce permissions while resolving related records and files.
- Add migrations only for authoritative system state that cannot safely live in schema/value JSONB.

Do not:

- Add arbitrary user-authored code.
- Treat file uploads as unprotected public URLs.
- Merge forms, records, reports, jobs, integrations, and workflows into one builder.
- Duplicate field types already present in the current schema.
- import credentials or execute source scripts from Creator-style exports.

## Verification Commands

```bash
dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj
dotnet build src/api/OpenBusinessPlatform.Api.csproj
cd src/app
npm test
npm run build
```

Run `git diff --check` before every commit.
