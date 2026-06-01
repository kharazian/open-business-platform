# Reports and Printing

Status: V1 browser print is implemented for record list/detail pages. V2 now has a saved list report definition builder with backend persistence, config validation, permission checks, runnable list report viewing over real record data, a real database-backed dashboard summary API, chart widget previews over permitted form/report data, and saved dashboard layouts. V2 has been expanded to use form data as the source for CSV export and cleaner print layouts.

## Principle

A form collects data.

A report displays data.

Keep report configuration separate from form configuration.

Reports and print output are the next bridge from the V1 form/record foundation toward automation. Workflows, triggers, validations, and scheduled actions all need the same reliable field metadata, record values, system fields, permissions, and form-version awareness that runnable reports require.

## Report Types

### List Report

Displays many records in a table.

V2 features:

- Select columns: implemented for saved definitions
- Reorder columns: not implemented yet
- Rename columns: labels are saved from the selected form fields
- Filters: one saved filter in the current builder, config supports multiple
- Sorting: one saved sort in the current builder, config supports multiple
- Search: implemented at runtime in the report viewer for visible searchable columns
- Saved reports: implemented
- Runnable viewer: implemented for saved list reports
- Print
- Export CSV

### Detail Report

Displays one record.

Can be used for:

- Record detail page
- Print single record
- Approval review
- PDF generation later

### Summary Report V2

Provides aggregation:

- Count
- Sum
- Average
- Group by department/status/date

### Dashboard V2

Displays widgets:

- Number cards
- Charts
- Tables
- Pending approvals later, after workflow/approval tasks

V2 dashboards now start with real database-backed workspace summaries, recent audit activity, previewable chart widgets, and saved custom dashboard layouts. Saved dashboards persist widget config and responsive layout JSON in PostgreSQL; workspace ownership is intentionally deferred to a later workspace module.

## V1 Printing

V1 supports browser print only:

- Print record detail
- Print current record list

Use CSS print styles where possible.

## V2 Printing

Add cleaner layouts:

- Header
- Footer
- Logo optional
- Date/time
- User name optional
- Report title
- Single-record detail print
- Report table print

Do single-record and report table printing before custom PDF templates. They prove the display contract that later PDF generation and workflow email attachments can reuse.

## V6 PDF and Templates

Later support:

- Custom print template builder
- PDF generation
- Conditional sections
- Page breaks
- Signature blocks
- Attach PDF to emails/triggers

## Report Config Example

```json
{
  "type": "list",
  "columns": [
    {
      "fieldId": "first_name",
      "label": "First Name",
      "visible": true,
      "width": 180
    }
  ],
  "filters": [
    {
      "fieldId": "status",
      "operator": "equals",
      "value": "active"
    }
  ],
  "sort": [
    {
      "fieldId": "created_at",
      "direction": "desc"
    }
  ]
}
```
