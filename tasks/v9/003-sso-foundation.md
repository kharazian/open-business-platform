# V9 Task 003: SSO Foundation

## Status

Complete.

## Goal

Add a workspace-scoped OpenID Connect (OIDC) sign-in foundation that preserves local login and never treats unvalidated external claims as an authenticated platform identity.

## Scope

- Persist workspace-owned OIDC provider configurations with issuer, client identifier, server-side client-secret reference, enabled state, and optimistic concurrency.
- Persist workspace-owned external identity links from a provider subject to an existing platform user.
- Add `users.manage`-protected provider management APIs with secret-safe responses and audit logs.
- Add anonymous provider discovery by explicit tenant/workspace slugs without making those request values an ownership authority.
- Implement authorization-code flow with PKCE, protected short-lived state, fixed callback handling, OIDC metadata/signing-key discovery, and issuer/audience/lifetime/nonce validation.
- Link only an existing active user with a verified matching email and an active membership in the target workspace.
- Issue the existing workspace-aware cookie and workspace-specific role/permission context after successful SSO.
- Add optional SSO buttons to the login page when a workspace is identified by URL query parameters.
- Preserve bootstrap and persistent local-user login behavior.

## Out Of Scope

- SAML, LDAP, SCIM, identity-provider initiated login, or logout federation.
- Automatic user or workspace-membership provisioning.
- Domain-based workspace discovery, custom domains, or workspace branding.
- Storing raw client secrets in PostgreSQL or returning secret values from APIs.
- Provider-specific group/role claim mapping and advanced RBAC/ABAC policy.
- Disabling local login or enforcing SSO-only policy.

## Security Rules

- OIDC providers and identity links are owned by exactly one workspace.
- Client secrets are resolved from server configuration by reference and are never persisted as provider values.
- State is integrity-protected, short-lived, single-flow scoped, and carries PKCE verifier and nonce material.
- Callback authentication validates issuer, signature, audience, lifetime, subject, nonce, and verified email before linking or signing in.
- Return paths must be local application paths; arbitrary redirect URLs are rejected.
- A valid external token is insufficient without an active local user and active target-workspace membership.
- Provider management and identity-link creation are audited without tokens, authorization codes, client secrets, or raw claims.

## Acceptance Criteria

- [x] OIDC provider and external identity entities are workspace-owned and migration-documented.
- [x] Provider management is backend-authorized, concurrency-safe, audited, and secret-safe.
- [x] Anonymous discovery exposes enabled provider display metadata only for an active tenant/workspace.
- [x] Authorization starts with PKCE, nonce, and protected expiring state.
- [x] Callback validates OIDC tokens and signs in only an existing active workspace member.
- [x] First successful verified-email login creates a unique auditable identity link; later logins resolve by issuer subject.
- [x] Local login remains available and unchanged.
- [x] Frontend login can discover and launch configured workspace SSO providers.
- [x] Backend harness/build, migration consistency, frontend tests/build, and `git diff --check` pass.
- [x] API, data model, architecture, security, roadmap, master PRD, and V9 handoff docs are updated.
