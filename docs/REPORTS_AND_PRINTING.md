# Reports and Printing

Status: V1 browser print is implemented for record list/detail pages. V2 now has a saved list report definition builder with backend persistence, config validation, and permission checks. V2 has been expanded to use form data as the source for runnable reports, real dashboard summaries, chart widgets, CSV export, and cleaner print layouts.

## Principle

A form collects data.

A report displays data.

Keep report configuration separate from form configuration.

## Report Types

### List Report

Displays many records in a table.

V2 features:

- Select columns: implemented for saved definitions
- Reorder columns: not implemented yet
- Rename columns: labels are saved from the selected form fields
- Filters: one saved filter in the current builder, config supports multiple
- Sorting: one saved sort in the current builder, config supports multiple
- Search: not implemented yet
- Saved reports: implemented
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

V2 dashboards should start with real database-backed summaries and chart widgets. Saved custom dashboard layouts can be added after report execution and chart aggregation are stable.

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
