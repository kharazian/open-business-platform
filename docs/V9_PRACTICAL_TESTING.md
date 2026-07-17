# V9 Practical Testing

Use this checklist to accept the V9 enterprise foundation in a clean environment.

## Deployment Order

1. Start PostgreSQL and Redis.
2. Apply every EF Core migration before starting the API:

   ```bash
   dotnet ef database update --project src/api/OpenBusinessPlatform.Api.csproj
   ```

3. Start the API and confirm `GET /health` returns `200`.
4. Start the frontend and open `http://127.0.0.1:5174`.

Starting API workers against an unmigrated database produces missing-table errors. Production deployment must therefore treat migration completion as an API startup prerequisite.

## Authentication And Isolation

1. Confirm `GET /api/auth/me` returns `401` without a session.
2. Sign in as a persisted workspace administrator and confirm these endpoints return `200`:
   - `/api/auth/me`
   - `/api/workspaces/current`
   - `/api/workspaces/available`
3. Switch between available workspaces, when a multi-workspace fixture is available, and confirm lists, settings, records, audit entries, and administration data never cross the signed active-workspace boundary.
4. Confirm a bootstrap recovery identity can administer the workspace but cannot save endpoints that require a persisted user, such as `PUT /api/localization/me`.

## Enterprise Administration

From Settings and Compliance & Audit, verify:

- workspace branding saves and immediately updates the login/app shell;
- workspace localization defaults and persisted-user overrides save independently;
- SSO provider configuration never exposes client secrets after saving;
- access-policy deny rules override matching allows;
- retention previews remain non-destructive and legal holds exclude protected records;
- a configuration backup downloads successfully and its restore plan is `valid` with `canApply: false`;
- a pending custom domain cannot be enabled and requests using that host fail closed;
- compliance posture, filtered audit results, and audit export remain workspace scoped.

## Acceptance Evidence — 2026-07-17

An isolated Compose project with fresh PostgreSQL and Redis volumes was exercised on alternate host ports. All 36 migrations applied through `20260717152713_CustomDomains`.

Observed results:

- health `200`; unauthenticated current-user request `401`; bootstrap and persisted-admin login `200`;
- current/available workspace, branding, localization, domains, compliance, retention, and backup list APIs `200`;
- branding save and host branding `200`;
- workspace and persisted-user localization saves `200`;
- custom-domain create `201`, pending-host request `404`, and pending-domain enable `400`;
- configuration backup create `201`, artifact download `200`, restore planning `200`, and restore status `valid`;
- the NuGet vulnerable-package scan reported no vulnerable direct or transitive packages after pinning `Microsoft.OpenApi` to `2.7.5`.

The acceptance run found and fixed a backup payload checksum canonicalization defect before final approval.

## Remaining External Checks

These need deployment-specific systems and are not represented by local green checks:

- a real OIDC provider round trip and account linking;
- public DNS TXT verification, reverse-proxy routing, and TLS issuance for a custom domain;
- production email delivery;
- scheduled retention operations against production-scale data;
- browser automation. Playwright is not installed in the current workspace, so the Settings and Compliance browser path remains a manual acceptance item.

