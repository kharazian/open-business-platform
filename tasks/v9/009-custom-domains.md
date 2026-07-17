# V9 Task 009: Custom Domains

## Status

Complete.

## Goal

Let workspaces prove and activate custom hostnames without allowing an unverified host header to select or override workspace ownership.

## Scope

- Persist globally unique, workspace-owned custom-domain registrations.
- Normalize internationalized DNS hostnames and reject IP, localhost, wildcard, URL, and port input.
- Issue DNS TXT verification challenges and verify them through a fixed DNS-over-HTTPS resolver.
- Support challenge rotation, verification checks, enable, and disable lifecycle actions with concurrency and audit logs.
- Resolve verified enabled request hosts before workspace-membership enforcement.
- Reject any custom host that conflicts with an authenticated signed workspace claim.
- Add authorized custom-domain administration to Settings.

## Out Of Scope

- TLS certificate issuance, proxy/DNS automation, apex redirects, path-based tenancy, arbitrary DNS providers, or deleting domain history.

## Safety Rules

- Host headers never activate pending, failed, or disabled registrations.
- DNS checks use a fixed resolver endpoint and never fetch a user-provided URL.
- Signed workspace claims cannot be overridden by a custom host.
- Lifecycle mutations require `domains.manage` and are audited.

## Acceptance Criteria

- [x] Domains are normalized, globally unique, workspace-owned, concurrency-safe, and audited.
- [x] Activation requires a matching DNS TXT proof.
- [x] Only verified enabled domains affect anonymous workspace discovery.
- [x] Conflicting authenticated host/workspace combinations fail closed.
- [x] Settings supports registration, verification, challenge rotation, enable, and disable.
- [x] Migration, backend harness/build, frontend tests/build, and `git diff --check` pass.
- [x] Architecture, API, data, security, deployment, roadmap, master PRD, and V9 handoff docs are updated.
