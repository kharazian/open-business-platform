# V8 Task 012: Trigger Outbox Operations

## Goal

Make transactional trigger-event delivery observable and recoverable without exposing record payloads.

## Context

Read:

- `docs/MASTER_PRD_FOR_AI.md`
- `docs/TRIGGERS_AND_WORKFLOWS.md`
- `docs/SECURITY_MODEL.md`
- `tasks/v8/011-transactional-trigger-event-outbox.md`
- `AGENTS.md`

## Requirements

- Provide form-scoped outbox health counts and oldest-pending timing to users with form `manage` permission.
- List bounded outbox delivery metadata without returning `payload_json`.
- Allow only dead-letter messages to be manually replayed.
- Reset replay delivery state atomically and write an audit entry with the acting user.
- Add a trigger workspace operations panel for health, dead letters, refresh, and replay.
- Remove completed outbox envelopes after a documented retention period without deleting dead letters.

## Acceptance Criteria

- [x] Unauthorized users cannot view or replay form outbox messages.
- [x] API and UI never expose outbox payload JSON.
- [x] Health reports pending, processing, completed, dead-letter, and oldest-pending metadata.
- [x] Concurrent replay requests cannot both reset the same dead letter.
- [x] Successful replay writes `trigger_event_outbox_replayed` audit metadata.
- [x] Completed messages older than the retention threshold are deleted in bounded batches.
- [x] Backend/frontend tests and builds pass.
- [x] API and automation documentation are updated.

## Status

Complete. Form managers can review delivery health and dead-letter metadata in the trigger workspace without receiving stored event payloads. Replay uses a conditional database update so only one request can reset a dead letter and writes a dedicated audit entry. A daily worker deletes at most 500 completed envelopes older than 30 days while retaining all unfinished and dead-letter messages.

## Out of Scope

- Editing event payloads before replay.
- Replaying completed or currently processing messages.
- Deleting dead letters automatically.
- External monitoring-provider integration.
