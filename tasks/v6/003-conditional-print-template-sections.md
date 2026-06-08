# V6 Task 003: Conditional Print Template Sections

## Goal

Allow record and report print template sections to show only when safe, field-based conditions match.

## Requirements

- Extend print template section config with optional visibility conditions.
- Support simple field-based operators:
  - equals
  - not equals
  - contains
  - is empty
  - is not empty
- Treat multiple conditions on a section as AND conditions.
- For record templates, evaluate conditions against the already-permission-filtered record schema and values used by the print renderer.
- For report templates, evaluate conditions against visible report execution columns/cells; a report section is visible when at least one visible row matches all conditions.
- Keep existing templates backward compatible by defaulting missing condition arrays to empty.
- Validate condition field ids and operators on the backend.
- Add matching frontend builder controls in `/printing`.
- Update docs to mark conditional sections as implemented in the browser-print foundation.

## Acceptance Criteria

- [x] Backend contracts and validation accept valid section conditions and reject unsupported operators or unknown fields.
- [x] Frontend template builder creates, normalizes, validates, and saves section conditions.
- [x] `/printing` exposes section condition controls.
- [x] Record template rendering hides sections when record conditions do not match.
- [x] Report template rendering hides sections when no visible report row matches the section conditions.
- [x] Existing templates without condition fields still render and can be edited.
- [x] Tests/builds pass.

## Out of Scope

- User-authored expressions or custom code.
- Cross-section logic.
- Conditional fields inside a section.
- Server-side binary PDF generation.
- PDF email attachments.
