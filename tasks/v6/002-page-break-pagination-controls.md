# V6 Task 002: Print Template Page Controls

## Goal

Add dependency-light page setup and page-break controls to record and report print templates.

## Requirements

- Extend print template config with document layout options:
  - Page size: Letter or A4.
  - Orientation: portrait or landscape.
  - Margin: narrow, normal, or wide.
  - Repeat table headers across printed pages.
- Extend section config with pagination controls:
  - Start section on a new page.
  - Keep section content together when practical.
- Keep existing templates backward compatible by applying default layout and section pagination values when config is missing them.
- Validate layout and pagination values on the backend.
- Add matching frontend builder controls in `/printing`.
- Render selected templates with print CSS classes that browsers can use for page setup and page-break behavior.
- Update docs to mark page-break and pagination controls as implemented in the browser-print foundation.

## Acceptance Criteria

- [x] Backend contracts and validation accept valid page controls and reject unsupported values.
- [x] Frontend template builder creates, normalizes, validates, and saves page controls.
- [x] `/printing` exposes page setup and per-section pagination controls.
- [x] Record/report template print rendering applies page setup and section page-break classes.
- [x] Existing templates without layout/pagination fields still render and can be edited.
- [x] Tests/builds pass.

## Out of Scope

- Server-side binary PDF generation.
- Exact total page count or page-number stamping, because browser print support is inconsistent.
- Drag-and-drop document designer.
- Template versioning.
