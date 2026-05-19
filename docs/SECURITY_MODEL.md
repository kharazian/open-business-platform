# Security Model

Status: evolving model. The current skeleton implements bootstrap-admin cookie authentication. Full authorization and persistent user management are not implemented yet.

## Core Rules

- Backend must enforce all permissions.
- Frontend checks are only for UX.
- Never return hidden field values to unauthorized users.
- Validate all submitted record values on the backend.
- Validate form schema changes on the backend.
- Use audit logs for sensitive actions.
- Use soft delete for important business records where possible.
- Do not allow users to update form version records after publish.

## API Security

Every API should verify:

- Authentication
- Authorization
- Input validation
- Resource ownership/access
- Field visibility if returning record values

## Permission Bypass Risks

Avoid these mistakes:

- Hiding buttons in React but leaving backend open.
- Returning all record values and hiding fields only in frontend.
- Exporting data without checking export permission.
- Printing data without checking print permission.
- Allowing form schema edits to published immutable versions.

## Audit Events

Log these actions:

- Form created
- Form published
- Record created
- Record updated
- Record deleted
- Record printed
- Report exported
- Permission changed
- Trigger executed
- Workflow transition later

## File Upload Security Later

If file upload is enabled:

- Validate file size
- Validate file type
- Scan or restrict risky file types
- Store files outside web root or behind authenticated access
- Check permissions before download
- Audit downloads for sensitive files
