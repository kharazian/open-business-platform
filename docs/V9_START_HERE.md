# V9 Start Here

This packet is the handoff for V9: Enterprise Platform.

## Current State

- V1 through V8 are complete for the current task lists.
- V8 finalized integrations and API foundations without adding tenant/workspace ownership, SSO, arbitrary custom code, public links, or broad external sync.
- The V8 finalization packet is `docs/V8_FINALIZATION.md`.
- V9 is intentionally postponed until the enterprise platform sequence is explicitly started.
- The working branch used for preparation was `dev`.

## Read In This Order

1. `AGENTS.md`
2. `docs/MASTER_PRD_FOR_AI.md`
3. `docs/ROADMAP.md`
4. `docs/V8_FINALIZATION.md`
5. `docs/API_SPEC.md`
6. `docs/DATA_MODEL.md`
7. `docs/TRIGGERS_AND_WORKFLOWS.md`
8. `tasks/v8/README.md`
9. `tasks/v9/README.md`
10. The selected `tasks/v9/*.md` task file when one is created.

## V9 Direction

V9 should mature the platform into an enterprise-ready product without weakening the module boundaries established in V1 through V8.

Recommended V9 order:

1. Workspace and tenant foundation.
2. Workspace-aware identity and membership.
3. SSO foundation.
4. Advanced RBAC/ABAC policy model.
5. Data retention and legal hold foundation.
6. Backup/restore and export administration.
7. White labeling and workspace branding.
8. Localization foundation.
9. Custom domains.
10. Compliance/audit administration.

## Scope Boundaries

Do:

- Keep enterprise ownership, identity, policy, retention, branding, localization, domains, and compliance as separate modules.
- Preserve backend authorization as the source of truth.
- Keep existing single-workspace development behavior working during migration.
- Add schema migrations only with documented migration notes.
- Add tests for permission, ownership, identity, retention, and audit behavior.

Do not:

- Rebuild forms, records, reports, dashboards, integrations, triggers, workflows, or printing as one giant enterprise module.
- Bypass existing permissions during workspace migration.
- Add SSO before workspace/user ownership boundaries are clear.
- Add custom domains before tenant/workspace routing and branding rules are explicit.
- Add destructive retention behavior without dry-run, audit, and recovery strategy.

## Verification Commands

Run these before committing implementation tasks:

```bash
dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj
dotnet build src/api/OpenBusinessPlatform.Api.csproj
cd src/app
npm test
npm run build
```

Run `git diff --check` before every commit.
