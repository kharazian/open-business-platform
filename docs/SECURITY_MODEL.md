# Security Model

Status: the established authentication, permission, field-security, and audit baseline now includes V9 task 004 deny-overrides enterprise policy guardrails.

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
- Restrict branding mutations to `branding.manage`; anonymous branding lookup accepts active tenant/workspace slugs but returns display fields only. Logo content is limited to bounded PNG/JPEG/WebP data URLs, colors to validated hex values, and every update is audited.
- Restrict workspace localization changes to `localization.manage`. Personal localization writes derive the user and workspace from the signed principal, accept only server-recognized cultures/timezones, and cannot select another user.
- Restrict custom-domain lifecycle changes to `domains.manage`. Hostnames are normalized and globally unique, activation requires a DNS TXT challenge checked through a fixed resolver, pending/disabled registrations do not route, and host resolution cannot override a conflicting signed workspace claim.
- Restrict compliance posture, audit search, and audit export to `compliance.manage`. Audit review never returns before/after payloads, redacts credential-like metadata keys, bounds date ranges/pages/exports, and writes an audit entry for every CSV export.
- Restrict Creator export analysis to cookie-authenticated callers with effective `forms.manage_all` and `integrations.manage`. Accept one memory-buffered UTF-8 text source up to 1 MiB/50,000 lines, never persist or execute it, detect credentials before report construction, and return categories plus typed platform-authored findings only. The aggregate audit event excludes filenames, names, snippets, URLs, values, and parser errors; every response has `canImport: false`.

## Workspace Boundary

V9 task 001 assigns every persisted business, permission, audit, automation, notification, and integration row to the active workspace. V9 task 002 adds explicit local-user memberships. Login selects an active membership, writes its workspace ID into the protected authentication ticket, and reloads role claims from that workspace. Middleware revalidates active user, membership, workspace, and tenant state on each cookie-authenticated request, so suspension takes effect without waiting for cookie expiration.

Users may list their memberships and request a workspace switch, but the backend accepts the target only when the user has an active membership. Switching replaces the signed workspace and role claims. Integration API-key principals carry the key's persisted workspace ID; request parameters never establish workspace ownership. Membership lifecycle changes require `users.manage`, optimistic concurrency, and audit logging.

## SSO Boundary

OIDC provider definitions and external identity links are workspace-owned. Provider client secrets are referenced by server configuration key and are never stored in PostgreSQL or returned through public APIs. Anonymous discovery exposes enabled provider IDs, keys, and display names only.

Authorization uses a short-lived data-protected state envelope, nonce, and PKCE S256 challenge. The callback retrieves trusted OIDC metadata and signing keys and validates issuer, signature, audience, lifetime, nonce, subject, and verified email. Successful provider authentication still requires an existing active platform user and active membership in the protected target workspace. The first verified login may link that user by the platform's unique normalized email; no user or membership is automatically provisioned. Return paths are restricted to local application paths, and local password login remains available.

## Enterprise Policy Guardrails

Task 004 evaluates existing RBAC grants and record scopes first, then applies enabled workspace access policies. Policies are deny-only and support platform, form, report, and record resources. Subject dimensions—role, membership role, department, and group—combine with AND, while values within a dimension combine with OR. Record policies may additionally match status and whether the current user owns the record.

Any matching policy denies access. Resource IDs may be omitted for workspace-wide coverage. Record conditions are translated into the existing EF query so denied rows do not enter application memory. The bootstrap recovery administrator is the only policy bypass; ordinary `Admin` role users remain subject to guardrails. Policy management and simulation require `roles.manage`, remain workspace-filtered, and policy mutations are audited.

## Retention Safety

Task 005 adds only `retention.manage`-protected definitions, legal holds, and dry-runs. Candidate evaluation stays database-side, excludes active holds, returns at most 100 IDs plus a count, and never returns payloads. No deletion, anonymization, or archival executor exists.

## Backup And Restore Safety

Task 006 protects snapshot administration with `backup.manage`; full record snapshots additionally require effective `forms.manage_all` and complete form/record export access after enterprise policy evaluation. Artifacts exclude credentials, hashes, secret-bearing extra properties, and previous artifact bodies, are capped at 25 MiB, and carry payload plus whole-artifact SHA-256 checksums. Downloads recompute checksums and write audit logs. Restore planning accepts only the current workspace's stored artifacts, validates format/modules/counts/checksums, reports ID conflicts, and always returns `canApply: false`.

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
