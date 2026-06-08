# V6 Task Index

V6 starts the print template and PDF foundation. The current slice keeps the platform dependency-light by storing reusable print templates, rendering professional browser-print/PDF documents for records and reports, and leaving server-side binary PDF generation and real email attachment delivery for later hardening after whole-system review.

## Recommended Execution Order

1. `001-print-template-foundation.md` - backend print template persistence, validation, permission-protected APIs, audit logs, frontend template management, record/report template rendering, and browser PDF generation.
2. `002-page-break-pagination-controls.md` - dependency-light browser print page setup, table header repetition, and section page-break controls.

## Scope Rules

- Keep print/PDF as its own module.
- Reuse existing record/report permission checks.
- Do not expose hidden fields through print template APIs or rendered output.
- Avoid a large server PDF dependency in this slice.
- Browser print/save-as-PDF is the V6 PDF foundation until server-side PDF generation is explicitly selected.
- Email trigger attachment metadata may be represented later, but actual PDF attachment delivery is out of scope for this foundation slice.
