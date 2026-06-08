# V6 Task 004: Print Template Versioning

## Goal

Add immutable published print template versions so browser print/PDF output can use stable snapshots while the template builder continues editing drafts.

## Requirements

- Store published print template snapshots in a separate `print_template_versions` table.
- Keep draft templates editable in `print_templates`.
- Add backend APIs to publish the current draft, list versions for a template, and read one published version.
- Require the same backend permissions used by print template management/viewing, including report-scoped permission checks.
- Add audit logs for publishing.
- Show publish controls and version history in `/printing`.
- Prefer the latest published version for record/report print selectors when one exists.
- Keep builder preview/editing on the draft template.

## Acceptance Criteria

- [x] Publishing creates the next sequential version number for a template.
- [x] Published version config, name, description, type, form scope, and report scope are stored separately from the draft.
- [x] Record and report print pages load the selected template's current published version if available.
- [x] Existing draft rendering remains available in the print template builder.
- [x] Backend model checks, frontend helper tests, backend build, frontend tests, and frontend build pass.

## Out Of Scope

- Server-side binary PDF generation.
- Reverting a draft to a previous version.
- Uploading logo/assets.
- Email trigger PDF attachments.
