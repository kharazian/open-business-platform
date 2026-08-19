# V10 Start Here

This packet is the handoff for V10: Operational App Modeling.

## Current State

- V1 through V9 are complete for their current task lists.
- V9 is accepted as the enterprise ownership, identity, policy, and administration checkpoint.
- V10 tasks 001 through 005 are complete; the next task requires a separately reviewed related-record workspace task file.
- Existing form schemas already support currency, percent, rating, URL, time, datetime, user/department pickers, record lookups, file placeholders, and child-record subtables.
- The next product gap is richer operational data modeling, not another enterprise administration layer.

## Read In This Order

1. `AGENTS.md`
2. `docs/MASTER_PRD_FOR_AI.md`
3. `docs/ROADMAP.md`
4. `docs/CREATOR_APP_SUPPORT_ROADMAP.md`
5. `docs/V9_FINALIZATION.md`
6. `docs/API_SPEC.md`
7. `docs/DATA_MODEL.md`
8. `tasks/v10/README.md`
9. The selected `tasks/v10/*.md` task file.

## Direction

V10 should make operational forms and records more expressive while preserving the existing schema/version, permission, report, workflow, and integration boundaries.

Recommended sequence:

1. Structured address fields.
2. Backend-generated autonumber fields (`tasks/v10/002-backend-generated-autonumber-fields.md`).
3. Real file attachment storage and protected access (`tasks/v10/003-protected-file-attachments.md`).
4. Stronger lookup relationship integrity (`tasks/v10/004-lookup-relationship-integrity.md`).
5. Nested relationship columns and filters in reports (`tasks/v10/005-nested-report-relationships.md`).
6. Related-record panels in record detail.
7. Typed operational report and row actions.
8. File-processing and scheduled job definitions.
9. Operational logs and failure notifications.
10. Analysis-only Creator-style export assistant.

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
