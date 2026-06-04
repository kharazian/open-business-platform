# V5 Task 001: Workflow Engine Foundation

## Goal

Add the backend foundation for workflow and approval definitions without introducing a visual builder.

## Context

Read:

- `docs/MASTER_PRD_FOR_AI.md`
- `docs/ROADMAP.md`
- `docs/TRIGGERS_AND_WORKFLOWS.md`
- `docs/ARCHITECTURE.md`
- `docs/API_SPEC.md`
- `docs/DATA_MODEL.md`
- `docs/PERMISSIONS.md`
- `tasks/v4/001-trigger-engine-foundation.md`
- `tasks/v4/008-create-related-record-trigger-action.md`
- `AGENTS.md`

## Requirements

- Add workflow definition persistence scoped to a form.
- Store workflow configuration as validated JSONB plus normalized metadata needed for listing, permissions, and history.
- Support draft/edit/publish-style workflow definitions or another explicit versioning model that keeps record history stable.
- Define workflow states, transitions, approval steps, assignee rules, and optional transition actions in typed backend contracts.
- Validate workflow definitions on create/update, including unique state keys, valid initial/final states, valid transition endpoints, and valid approval assignee rules.
- Add backend management APIs to list, create, read, update, enable, disable, and publish workflow definitions.
- Add workflow history persistence for future record transition execution.
- Add permission checks for workflow management.
- Add audit logs for workflow create/update/publish/enable/disable actions.
- Seed one simple workflow only if it improves local smoke testing and does not obscure the existing demo data.
- Keep record transition execution, approval inbox, notifications, XYFlow, scheduled starts, and workflow-trigger integration out of scope.

## Acceptance Criteria

- [x] Database migrations add workflow definition and workflow history foundation tables.
- [x] Backend DTOs represent states, transitions, approval steps, assignee rules, and definition status without using `any`-style untyped payloads.
- [x] Backend validation rejects duplicate states, missing initial state, invalid transition endpoints, invalid approval steps, and invalid assignee rules.
- [x] Backend exposes permission-protected workflow definition management APIs.
- [x] Backend writes audit logs for workflow definition mutations.
- [x] Workflow definitions can be enabled/disabled without deleting history.
- [x] Published/versioned workflow definitions are immutable enough for future record history to reference safely.
- [x] Existing trigger, form, record, report, notification, and permission behavior is not broken.
- [x] Documentation is updated for new tables and APIs.
- [x] Relevant tests/builds are run.

## Current Status

Completed for the backend foundation slice. V5 task 001 now has form-scoped workflow definition persistence, draft config storage, immutable publish/version rows, typed backend contracts, definition validation, management APIs, form-manage permission checks, mutation audit logs, workflow history foundation tables, and backend harness coverage.

## Out of Scope

- XYFlow or visual workflow diagrams.
- Frontend workflow builder UI.
- Record transition execution.
- Approval inbox.
- Workflow notifications.
- Trigger-to-workflow starts.
- Scheduled workflow starts.
- PDF generation or print templates.
- Webhooks and external integrations.
- Custom code execution.

## Tests

- Add backend harness coverage for workflow entity/model mapping.
- Add backend harness coverage for workflow definition validation.
- Add backend harness coverage for workflow permission contract constants.
- Add backend harness coverage for API route/DTO shape where practical in the current lightweight harness.
- Run `dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj`.
- Run `dotnet build src/api/OpenBusinessPlatform.Api.csproj`.
- Run `npm test` and `npm run build` in `src/app` if frontend contracts or docs references change frontend code.

## Notes

- Start table/form based. XYFlow belongs later, after the backend workflow contract has proven itself.
- Prefer a versioning model that mirrors published forms: records and workflow history should be able to point at the exact workflow definition version used at the time.
- Keep workflow actions typed and compatible with the existing trigger action direction, but avoid executing actions until a later transition-execution task.
