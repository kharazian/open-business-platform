# V9 Task Index

V9 is the Enterprise Platform sequence. It can be postponed until V8 has been accepted as the stable integrations/API checkpoint.

The sequence below is a planning list only. Create a specific task file before implementation begins.

## Recommended Execution Order

1. `001-workspace-and-tenant-foundation.md` - complete; tenant/workspace entities, stable default ownership, safe migration backfill, central query filters/write guards, and current-context API.
2. `002-workspace-membership-and-user-context.md` - complete; workspace membership, signed active context, invitation/activation/suspension rules, workspace switching, and backend authorization checks.
3. `003-sso-foundation.md` - complete; workspace-scoped OIDC providers, protected authorization-code/PKCE flow, validated external identity links, and optional workspace login buttons.
4. `004-advanced-rbac-abac-policy-model.md` - complete; typed workspace deny policies now layer role, membership-role, department, group, status, and ownership conditions over existing grants.
5. `005-data-retention-and-legal-hold.md` - complete; non-destructive retention definitions, legal holds, and payload-free dry-run evaluation.
6. `006-backup-restore-and-admin-export.md` - complete; protected workspace snapshots, checksummed manifests, audited downloads, and validation-only restore plans.
7. `007-white-labeling-and-workspace-branding.md` - complete; workspace-owned app labels, safe logo data, primary color, login copy, public projection, audited administration, and real-app integration remain separate from user appearance preferences.
8. `008-localization-foundation.md` - add locale, timezone, date/number formatting, and translatable label foundations.
9. `009-custom-domains.md` - add custom domain configuration after workspace routing and branding are stable.
10. `010-compliance-and-audit-administration.md` - add compliance reporting, audit retention review, and sensitive admin activity surfaces.

## Scope Rules

- Start with ownership and workspace context before SSO, custom domains, or compliance surfaces.
- Keep backend authorization authoritative for every enterprise feature.
- Preserve compatibility for existing development seed data and single-workspace usage.
- Avoid destructive retention behavior until backup/restore and audit paths are clear.
- Do not introduce cross-tenant data access shortcuts.
- Document migrations and update API/data model docs for every enterprise table or endpoint.
