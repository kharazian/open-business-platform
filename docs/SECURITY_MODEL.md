# Security Model

Status: the established authentication, permission, field-security, and audit baseline now includes V9 task 002 workspace membership and request identity enforcement.

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
- Keep monitoring output aggregate and payload-free; production metrics require a server-side token that is never exposed through Vite variables.
- Resolve workspace context on the backend; never trust a request body, query string, or frontend-only selection as ownership authority.
- Apply workspace filters to direct reads and reject cross-workspace creates, updates, deletes, and ownership changes centrally in persistence.

## Workspace Boundary

V9 task 001 assigns every persisted business, permission, audit, automation, notification, and integration row to the active workspace. V9 task 002 adds explicit local-user memberships. Login selects an active membership, writes its workspace ID into the protected authentication ticket, and reloads role claims from that workspace. Middleware revalidates active user, membership, workspace, and tenant state on each cookie-authenticated request, so suspension takes effect without waiting for cookie expiration.

Users may list their memberships and request a workspace switch, but the backend accepts the target only when the user has an active membership. Switching replaces the signed workspace and role claims. Integration API-key principals carry the key's persisted workspace ID; request parameters never establish workspace ownership. Membership lifecycle changes require `users.manage`, optimistic concurrency, and audit logging.

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
