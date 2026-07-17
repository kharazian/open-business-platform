# V10 Task Index

V10 is the Operational App Modeling sequence. It starts only after the accepted V9 checkpoint.

## Recommended Execution Order

1. `001-structured-address-field.md` - complete; typed structured addresses now span schema validation, builder, renderer, records, reports, CSV, and print-safe display.
2. Autonumber fields - create a task file before implementation; define backend-only allocation and concurrency semantics.
3. File attachment storage - create a task file before implementation; replace string placeholders with metadata, storage abstraction, scanning hooks, and protected downloads.
4. Lookup relationship integrity - create a task file before implementation; define delete/archive behavior and permission-safe relationship validation.
5. Nested report relationships - create a task file before implementation; add bounded lookup paths, typed filters, and hidden-field protection.
6. Related-record workspace - create a task file before implementation; add permission-aware related panels to record detail.
7. Operational report actions - create a task file before implementation; add typed report and row actions without client scripts.
8. Processing jobs and schedules - create a task file before implementation; add bounded job definitions, claims, runs, and retry controls.
9. Operational logs and notifications - create a task file before implementation; keep diagnostics separate from audit history and deduplicate failure alerts.
10. Creator-style export analysis - create a task file before implementation; produce a redacted compatibility report without mutating platform data.

## Scope Rules

- Implement one task at a time.
- Keep schemas separate from UI components.
- Keep form rendering separate from form building.
- Preserve immutable published versions and record `form_version_id` references.
- Keep PostgreSQL and backend authorization authoritative.
- Reuse existing record, report, permission, trigger, workflow, printing, audit, notification, and integration services.
- Do not add arbitrary code execution or unsafe public artifacts.
