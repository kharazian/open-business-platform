# V9 Task Index

V9 is the Enterprise Platform sequence. It can be postponed until V8 has been accepted as the stable integrations/API checkpoint.

The sequence below is a planning list only. Create a specific task file before implementation begins.

## Recommended Execution Order

1. `001-workspace-and-tenant-foundation.md` - define tenant/workspace entities, ownership boundaries, default workspace behavior, and migration strategy for existing data.
2. `002-workspace-membership-and-user-context.md` - add workspace membership, active workspace context, invitation/activation rules, and backend authorization checks.
3. `003-sso-foundation.md` - introduce SSO configuration contracts and login flow boundaries after workspace ownership exists.
4. `004-advanced-rbac-abac-policy-model.md` - extend roles and scoped permissions with enterprise policy rules while preserving existing permission semantics.
5. `005-data-retention-and-legal-hold.md` - add retention policy definitions, dry-run evaluation, legal hold exclusions, and audit logs.
6. `006-backup-restore-and-admin-export.md` - add administrative export/backup contracts and safe restore planning without weakening record permissions.
7. `007-white-labeling-and-workspace-branding.md` - persist workspace branding, app labels, logos, and theme defaults separately from user appearance preferences.
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
