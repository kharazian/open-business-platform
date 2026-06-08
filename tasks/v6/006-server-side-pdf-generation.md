# V6 Task 006: Server-Side PDF Generation

## Goal

Generate downloadable server-side PDF files from published print template versions for permitted record and report data.

## Requirements

- Generate `application/pdf` bytes without adding a large PDF/rendering dependency.
- Use published print template versions, not editable drafts.
- Reuse existing record/report permission and field-filtering services.
- Support record templates over one record.
- Support report templates over a permitted report execution page.
- Add backend audit logs for server-side PDF generation.
- Add frontend download helpers and use server downloads when a selected template has a published version.
- Keep browser print available for default layouts and unpublished draft template previews.

## Acceptance Criteria

- [x] Backend PDF writer returns a valid PDF byte envelope.
- [x] Record PDF generation uses the published template version and permission-filtered record values.
- [x] Report PDF generation uses the published template version and permission-filtered report rows.
- [x] Record/report pages download server PDFs for published selected templates.
- [x] Browser print still works for default layouts and unpublished draft templates.
- [x] Tests/builds pass.

## Out of Scope

- Pixel-perfect browser CSS rendering.
- Embedded logo image drawing.
- Server-side HTML rendering.
- PDF email attachments.
- Long-running background PDF jobs.
