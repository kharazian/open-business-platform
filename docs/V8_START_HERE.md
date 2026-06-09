# V8 Start Here

This packet is the handoff for V8: Integrations and API.

## Current State

- V1 through V7 are complete for the current task lists.
- V7 task 004 added conservative dashboard visibility/default metadata and closed the current dashboard analytics sequence.
- V8 should begin with integration authentication before exposing inbound webhooks, imports, exports, or broader public/internal APIs.
- The working branch used for preparation was `dev`.

## Read In This Order

1. `AGENTS.md`
2. `docs/MASTER_PRD_FOR_AI.md`
3. `docs/ROADMAP.md`
4. `docs/API_SPEC.md`
5. `docs/DATA_MODEL.md`
6. `docs/TRIGGERS_AND_WORKFLOWS.md`
7. `tasks/v4/010-webhooks-retry-policies-scheduled-triggers.md`
8. `tasks/v5/006-trigger-to-workflow-starts.md`
9. `tasks/v7/README.md`
10. `tasks/v8/README.md`
11. The selected `tasks/v8/*.md` task file.

## V8 Direction

V8 connects the platform to external systems without weakening the existing permission, audit, and module boundaries.

Implement V8 in this order:

1. API keys and integration auth.
2. Integration logs and retry foundation.
3. Public/internal record API foundation.
4. Incoming webhook listeners.
5. Record import jobs.
6. External export jobs.
7. Scheduled automation expansion.
8. Scheduled workflow starts.
9. Integration operations UI.

## Scope Boundaries

Do:

- Store API keys hashed, never in plaintext.
- Enforce backend permissions for all integration entry points.
- Add audit/integration logs for sensitive integration actions.
- Keep integrations separate from reports, dashboards, triggers, workflows, and print modules.
- Prefer small typed contracts over arbitrary custom code or raw SQL.
- Add retry behavior only through explicit, observable integration logs.

Do not:

- Add custom code execution.
- Add arbitrary SQL, cross-form joins, or broad data sync.
- Add public links or anonymous data access.
- Bypass form/report/field/record permissions.
- Replace existing trigger/webhook action behavior.
- Build multi-tenant workspace ownership in V8.

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
