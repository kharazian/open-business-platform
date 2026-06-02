# V4 Task 004: Trigger Retry Recovery

## Goal

Add a safe manual retry path for failed trigger executions.

## Context

Read:

- `docs/MASTER_PRD_FOR_AI.md`
- `docs/ROADMAP.md`
- `docs/TRIGGERS_AND_WORKFLOWS.md`
- `docs/API_SPEC.md`
- `tasks/v4/001-trigger-engine-foundation.md`
- `tasks/v4/002-trigger-management-ui.md`
- `tasks/v4/003-update-field-trigger-action.md`
- `AGENTS.md`

## Requirements

- Add a management-only API endpoint to retry one failed trigger log.
- Retry only logs with `failed` status.
- Replay the saved trigger event input against the same trigger action list.
- Create a new trigger execution log for the retry attempt.
- Include retry metadata that links the new log to the failed log.
- Preserve current failure behavior: retry failures are logged and do not roll back unrelated work.
- Add a retry action to the trigger logs UI for failed logs.

## Acceptance Criteria

- [x] Backend rejects retry requests for missing logs, logs from another trigger, and non-failed logs.
- [x] Backend retry creates a new trigger execution log using the saved input from the failed log.
- [x] Backend retry logs success or failure with retry metadata.
- [x] Backend retry APIs require form `manage` or `forms.manage_all` access.
- [x] Frontend API client can call the retry endpoint.
- [x] Trigger logs UI shows a retry button only for failed logs.
- [x] Retrying refreshes the selected trigger logs.
- [x] Documentation is updated if contracts or trigger behavior change.
- [x] Relevant tests/builds are run.

## Current Status

Completed for the current V4 recovery slice. Failed trigger logs can be retried manually by form managers; each retry creates a fresh execution log with retry metadata linking it to the failed source log.

## Out of Scope

- Automatic background retries.
- Retry queues.
- Scheduled triggers.
- Webhook triggers.
- Retry policy authoring.
- Cross-trigger replay.
- Custom code execution.

## Tests

- Add frontend API client tests for the retry endpoint.
- Add backend harness coverage for retry contract/status shape.
- Run `npm test` and `npm run build` in `src/app`.
- Run `dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj`.
- Run `dotnet build src/api/OpenBusinessPlatform.Api.csproj`.
