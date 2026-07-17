# Task: Structured Address Field

## Goal

Add a first-class structured address field that can be designed, versioned, submitted, edited, displayed, and safely reported without flattening the value into an ambiguous string.

## Context

Read:

- `AGENTS.md`
- `docs/MASTER_PRD_FOR_AI.md`
- `docs/ARCHITECTURE.md`
- `docs/API_SPEC.md`
- `docs/DATA_MODEL.md`
- `docs/CREATOR_APP_SUPPORT_ROADMAP.md`
- `docs/V10_START_HERE.md`
- `tasks/v10/README.md`

The current schema already supports many practical business field types. Address is intentionally the first V10 addition because it requires a bounded structured record value and establishes patterns needed by future composite fields without requiring a new database table.

## Requirements

- Add an `address` field type to matching frontend and backend schema contracts.
- Define a bounded value with optional `line1`, `line2`, `city`, `region`, `postalCode`, `country`, `latitude`, and `longitude` members.
- Allow field configuration to select required subfields while retaining the existing top-level required behavior.
- Validate member types, lengths, latitude/longitude ranges, unknown members, and configured required members on the backend; mirror deterministic validation in frontend helpers.
- Render semantic address inputs in the form renderer and record editor with accessible labels and responsive layout.
- Add builder controls for address subfield requirements without placing business logic in the React page.
- Store the structured value in existing record `values_json`; do not add a database column or table.
- Produce a stable, human-readable display value for record lists, details, reports, CSV, print, and audit-safe summaries without exposing hidden fields.
- Preserve existing published schemas and records unchanged.

## Acceptance Criteria

- [ ] Draft and publish validation accept valid address configuration and reject unsupported members.
- [ ] Record create/edit validation accepts valid structured addresses and returns path-specific errors for invalid members.
- [ ] Required subfields are enforced by the backend.
- [ ] Builder, renderer, preview, submission, record edit, list/detail, report, CSV, and print paths handle address values without displaying `[object Object]`.
- [ ] Existing primitive field values and schema version 1 remain compatible.
- [ ] Frontend and backend tests cover valid, invalid, optional, and required address cases.
- [ ] API/data-model documentation describes the contract and JSONB storage.
- [ ] Backend harness/build, frontend tests/build, and `git diff --check` pass.

## Out of Scope

- Geocoding, maps, address autocomplete, or third-party address validation.
- A country/region reference-data service.
- Nested address columns or operators in the report builder beyond a safe combined display value.
- Autonumber, attachment storage, lookup integrity, or other V10 field work.
- A schema-version migration solely for the additive field type.

## Tests

- Extend backend form-schema and record-value validation coverage.
- Extend frontend schema, builder, renderer, and display-formatting tests.
- Verify legacy schemas deserialize and validate unchanged.

## Notes

- Keep the address contract bounded and explicit; do not accept arbitrary nested JSON.
- Use invariant numeric rules for coordinates and localization only for presentation.
- Implement only this task.

