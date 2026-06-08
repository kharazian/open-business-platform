# V6 Task Index

V6 completes the print template and PDF foundation with dependency-light reusable print templates, browser print/save-as-PDF output, server-side PDF downloads, and record PDF attachments for trigger email actions.

## Recommended Execution Order

1. `001-print-template-foundation.md` - backend print template persistence, validation, permission-protected APIs, audit logs, frontend template management, record/report template rendering, and browser PDF generation.
2. `002-page-break-pagination-controls.md` - dependency-light browser print page setup, table header repetition, and section page-break controls.
3. `003-conditional-print-template-sections.md` - field-based section visibility conditions for record and report templates.
4. `004-print-template-versioning.md` - immutable published print template snapshots, publish/history APIs, builder publish controls, and latest-published rendering for record/report prints.
5. `005-print-template-logo-uploads.md` - safe small logo uploads stored as template header data URLs, with preview/remove controls and logo source validation.
6. `006-server-side-pdf-generation.md` - dependency-light server PDF generation for published record/report print template versions, with audit logs and frontend downloads.
7. `007-pdf-email-attachments.md` - record PDF attachments for trigger `send_email` actions using published same-form record print templates.

## Scope Rules

- Keep print/PDF as its own module.
- Reuse existing record/report permission checks.
- Do not expose hidden fields through print template APIs or rendered output.
- Avoid a large server PDF dependency in this slice.
- Browser print/save-as-PDF remains available for default layouts and unpublished draft template previews.
- Trigger email PDF attachments are limited to record triggers with a current record context and one published same-form record print template.
- Scheduled trigger PDF attachments, report PDF attachments, background jobs, and per-recipient field-level rendering remain out of scope for V6.
