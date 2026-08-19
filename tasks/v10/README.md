# V10 Task Index

V10 is the Operational App Modeling sequence. It starts only after the accepted V9 checkpoint.

## Recommended Execution Order

1. `001-structured-address-field.md` - complete; typed structured addresses now span schema validation, builder, renderer, records, reports, CSV, and print-safe display.
2. `002-backend-generated-autonumber-fields.md` - complete; immutable formatted identifiers now use PostgreSQL-backed transactional allocation and read-only record values.
3. `003-protected-file-attachments.md` - complete; file fields now use workspace metadata, storage/scanning boundaries, atomic record claims, safe filename display, and protected downloads.
4. `004-lookup-relationship-integrity.md` - complete; validated lookups now materialize canonical edges with archive-safe selection and referenced-delete protection.
5. `005-nested-report-relationships.md` - complete; permission-safe one-hop lookup columns now support typed filters, search, sort, viewer, CSV, and print paths with compatible dotted field keys.
6. `006-related-record-workspace.md` - complete; permission-aware, read-only reverse lookup panels now provide canonical/legacy discovery, independently paged display rows, and resilient record-detail states.
7. `007-typed-operational-report-actions.md` - complete; saved typed report/row actions now have backend validation, permission-projected availability, fixed-catalog builder controls, and ordered viewer rendering without a generic action executor.
8. `008-bounded-processing-jobs-and-schedules.md` - complete; adds bounded workspace processing definitions/runs, manual CSV imports, scheduled protected exports, fenced claims, and safe export retries.
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
