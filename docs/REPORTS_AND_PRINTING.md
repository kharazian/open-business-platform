# Reports and Printing

Status: planned. Current reports pages are starter/sample UI. Real report builder work begins in V2 after V1 forms and records are stable.

## Principle

A form collects data.

A report displays data.

Keep report configuration separate from form configuration.

## Report Types

### List Report

Displays many records in a table.

V2 features:

- Select columns
- Reorder columns
- Rename columns
- Filters
- Sorting
- Search
- Saved reports
- Print
- Export CSV

### Detail Report

Displays one record.

Can be used for:

- Record detail page
- Print single record
- Approval review
- PDF generation later

### Summary Report Later

Provides aggregation:

- Count
- Sum
- Average
- Group by department/status/date

### Dashboard Later

Displays widgets:

- Number cards
- Charts
- Tables
- Pending approvals

## V1 Printing

V1 should support browser print only:

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
