# V8 Start Here

This packet is the handoff for V8: Integrations and API.

## Current State

- V1 through V7 are complete for the current task lists.
- V7 task 004 added conservative dashboard visibility/default metadata and closed the current dashboard analytics sequence.
- V8 task 001 added hashed integration API key management and API-key authentication plumbing before exposing inbound webhooks, imports, exports, or broader public/internal APIs.
- V8 task 002 added integration log persistence, sanitized metadata, and explicit retry request metadata before adding more integration surfaces.
- V8 task 003 added versioned API-key-authenticated record list/read/create endpoints that reuse existing backend permissions and field rules.
- V8 task 004 added named incoming webhook listeners with hashed listener secrets, API-key or listener-secret authentication, typed payload mappings, create/safe-lookup upsert record execution, and inbound integration logs.
- V8 task 005 added CSV record import jobs with explicit field mappings, persisted job/row status, row-level validation results, audit logs, and inbound import integration logs.
- V8 task 006 added external export jobs for permission-filtered form records and list reports with CSV/JSON artifacts, hidden-field filtering, audit logs, and outbound export integration logs.
- V8 task 007 added explicit daily/weekly/monthly scheduled automation contracts with interval/day metadata, tested due-time calculation, safe scheduled-action validation, and schedule run metadata in trigger logs.
- V8 task 008 added scheduled workflow starts with explicit same-form workflow targets, bounded record selection rules, workflow history/audit writes, and trigger log record results.
- V8 task 009 added a permission-aware `/integrations` operations UI for API keys, integration logs, and explicit retry requests.
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

1. API keys and integration auth. Complete.
2. Integration logs and retry foundation. Complete.
3. Public/internal record API foundation. Complete.
4. Incoming webhook listeners. Complete.
5. Record import jobs. Complete.
6. External export jobs. Complete.
7. Scheduled automation expansion. Complete.
8. Scheduled workflow starts. Complete.
9. Integration operations UI. Complete.

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
