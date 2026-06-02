# Security Model

Status: V3 security baseline complete for the current repository. The platform implements bootstrap-admin cookie authentication, local PostgreSQL user login, self-service password recovery for persistent users, persistent users/roles/groups/departments management, scoped record permissions, report permissions, and backend authorization checks for auth, Users & Access, dashboard, forms, records, reports, chart previews, and field-level hidden/read-only rules.

## Core Rules

- Backend must enforce all permissions.
- Frontend checks are only for UX.
- Never return hidden field values to unauthorized users.
- Validate all submitted record values on the backend.
- Validate form schema changes on the backend.
- Use audit logs for sensitive actions.
- Use soft delete for important business records where possible.
- Do not allow users to update form version records after publish.
- Store only hashes of password reset tokens, use generic forgot-password responses, and expire/mark reset tokens as used.

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
- Treating browser draft state as a persisted or publishable backend schema.

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
- Password reset requested
- Password reset completed
- Trigger executed
- Workflow transition later

## V3 Field Security

Hidden field values are removed from API responses instead of being returned as `null`. Read-only fields are enforced on the backend during record edits, so changing the browser payload cannot bypass the UI.

## File Upload Security Later

If file upload is enabled:

- Validate file size
- Validate file type
- Scan or restrict risky file types
- Store files outside web root or behind authenticated access
- Check permissions before download
- Audit downloads for sensitive files
