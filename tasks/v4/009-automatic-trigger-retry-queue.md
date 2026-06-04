# V4 Task 009: Automatic Trigger Retry Queue

## Goal

Add a safe automatic retry foundation for failed trigger executions, building on the existing manual retry behavior.

## Context

Read:

- `docs/MASTER_PRD_FOR_AI.md`
- `docs/ROADMAP.md`
- `docs/TRIGGERS_AND_WORKFLOWS.md`
- `docs/API_SPEC.md`
- `docs/DATA_MODEL.md`
- `tasks/v4/001-trigger-engine-foundation.md`
- `tasks/v4/004-trigger-retry-recovery.md`
- `tasks/v4/007-notification-badges-and-preferences.md`
- `AGENTS.md`

## Requirements

- Persist retry scheduling metadata for failed trigger logs or retry queue rows.
- Add a bounded retry policy for automatic retries, with a safe default maximum attempt count.
- Schedule retries only for failed executions whose trigger remains enabled.
- Replay the saved trigger event input through the trigger's current action list, matching manual retry semantics.
- Link automatic retry logs to their failed source log.
- Ensure retries are idempotent at the queue/log level so one failed log is not retried concurrently by multiple workers.
- Add a management API or UI signal that shows whether a failed log has automatic retries pending, exhausted, or disabled.
- Keep Redis/background worker infrastructure optional unless the existing backend already has the needed hosted-service pattern.
- Keep schedules, webhook triggers, custom retry policies in the UI, and custom code execution out of scope.

## Acceptance Criteria

- [x] Failed trigger executions can be marked for automatic retry with next-attempt metadata.
- [x] A hosted retry worker or equivalent backend service processes due retry attempts.
- [x] Automatic retries create fresh trigger logs linked to the failed source log.
- [x] Retry attempts stop after the configured maximum attempt count.
- [x] Disabled triggers are not retried automatically.
- [x] Concurrent retry processing cannot duplicate the same retry attempt.
- [x] Trigger logs UI or API responses expose retry pending/exhausted state.
- [x] Manual retry behavior continues to work.
- [x] Documentation is updated if contracts, schemas, or runtime behavior change.
- [x] Relevant tests/builds are run.

## Current Status

Completed. Failed first-attempt trigger executions now persist automatic retry metadata on `trigger_logs`, the backend hosted retry worker claims due failed logs atomically before replaying them, disabled triggers are skipped, retry attempts stop at the default maximum of three, and the trigger logs API/UI expose retry state.

## Out of Scope

- User-authored retry policies.
- Scheduled triggers.
- Webhook triggers.
- Workflow retries.
- Distributed job dashboard.
- Push notifications or websocket updates.
- Custom code execution.

## Tests

- Add backend harness coverage for retry metadata mapping and defaults.
- Add backend harness coverage for maximum-attempt behavior.
- Add backend harness coverage that disabled triggers are skipped.
- Add backend harness coverage for retry log linkage.
- Add frontend API/UI helper tests if retry state is surfaced in the trigger logs UI.
- Run `dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj`.
- Run `dotnet build src/api/OpenBusinessPlatform.Api.csproj`.
- Run `npm test` and `npm run build` in `src/app`.

## Notes

- Start with a conservative default policy, such as three attempts with simple backoff, unless the implementation plan chooses a different documented default.
- The retry worker must prefer correctness and observability over throughput.
