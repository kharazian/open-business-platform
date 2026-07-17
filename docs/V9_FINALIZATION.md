# V9 Finalization

V9 Enterprise Platform is complete for tasks 001 through 010.

## Delivered

1. Tenant/workspace ownership and centralized isolation.
2. Workspace memberships, signed active context, and switching.
3. Workspace-scoped OIDC SSO foundation.
4. Deny-overrides enterprise access policies.
5. Retention definitions, legal holds, and non-destructive dry runs.
6. Checksummed administrative snapshots and validation-only restore plans.
7. Safe workspace branding and login/app integration.
8. Workspace localization defaults, user overrides, and shared formatting helpers.
9. DNS-verified custom domains with fail-closed signed-workspace matching.
10. Operational compliance posture and payload-safe audit administration.

## Boundaries Preserved

- Backend authorization and PostgreSQL remain authoritative.
- `/theme` remains a sample-data playground.
- User appearance remains separate from workspace branding/localization.
- Retention and restore execution remain intentionally non-destructive/validation-only.
- Custom-domain TLS/proxy automation remains a deployment responsibility.
- Compliance posture is operational evidence, not certification or legal advice.

## Verification Gate

Each task was committed only after backend harness/build, frontend tests/build, migration consistency where applicable, and `git diff --check`.

Final acceptance also covered a clean isolated PostgreSQL/Redis environment, all 36 migrations, authentication boundaries, enterprise administration APIs, custom-domain fail-closed behavior, and checksummed backup/restore planning. The acceptance run found and fixed backup payload checksum canonicalization. See `docs/V9_PRACTICAL_TESTING.md` for the evidence and remaining deployment-specific checks.

The `Microsoft.OpenApi` dependency is pinned to patched version `2.7.5`; the final direct/transitive vulnerability scan reports no vulnerable packages.

## Next

V9 is accepted as a checkpoint. Open a separately scoped V10 plan before adding product scope.
