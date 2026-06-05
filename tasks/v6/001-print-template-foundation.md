# V6 Task 001: Print Template Foundation

## Goal

Add a professional print template foundation for record and report documents.

## Requirements

- Persist print templates in PostgreSQL with form/report scope and JSONB config.
- Expose permission-protected backend APIs to list, create, read, update, and soft-delete templates.
- Validate template type, name, scope, layout options, fields, header/footer/logo, conditional sections, and signature blocks.
- Add audit logs for template creation, update, and deletion.
- Add a real frontend `/printing` workspace for template management.
- Render selected templates for record detail and report viewer prints.
- Provide “Generate PDF” actions through browser print/save-as-PDF.
- Update documentation for the V6 foundation and future server-side PDF/attachment limits.

## Acceptance Criteria

- [x] Database migration adds `print_templates`.
- [x] Backend APIs enforce authentication, print/report/form permissions, validation, and audit.
- [x] Frontend can create and edit record/report templates.
- [x] Record detail print can use a selected record template.
- [x] Report viewer print can use a selected report template.
- [x] Browser PDF actions use the same rendered template output.
- [x] Tests/builds/EF checks pass.

## Out of Scope

- Server-side binary PDF generation.
- Uploading logo files.
- Actual email attachment delivery.
- Template versioning.
- Drag-and-drop document designer.
