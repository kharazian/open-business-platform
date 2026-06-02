# V4 Task 002: Trigger Management UI

## Goal

Add a frontend workspace for managing V4 backend trigger definitions and reviewing trigger execution logs.

## Context

Read:

- `docs/MASTER_PRD_FOR_AI.md`
- `docs/ROADMAP.md`
- `docs/TRIGGERS_AND_WORKFLOWS.md`
- `docs/API_SPEC.md`
- `docs/UI_SPEC.md`
- `docs/CODING_STANDARDS.md`
- `tasks/v4/001-trigger-engine-foundation.md`
- `AGENTS.md`

## Requirements

- Add a real Triggers navigation item and route.
- Let users select a form and view that form's triggers.
- Let users create and update trigger definitions using the existing backend-supported event, condition, and action types.
- Let users enable or disable a trigger through the update endpoint.
- Let users inspect execution logs for the selected trigger.
- Keep trigger definitions and execution logs backend-owned.
- Keep triggers separate from workflows.
- Enforce backend permissions through the existing API; frontend navigation may hide UI but must not be the source of authorization.

## Acceptance Criteria

- [x] Frontend trigger API client supports list, detail, create, update, and log requests.
- [x] Trigger builder UI supports V4 task 001 events: `record.created`, `record.updated`, `field.changed`, `status.changed`, and `record.assigned`.
- [x] Trigger builder UI supports V4 task 001 conditions: field equals, field changed, status changed to, department equals, assigned user, and assigned group.
- [x] Trigger builder UI supports V4 task 001 actions: write audit entry, send email, change status, and assign record.
- [x] Trigger list displays trigger name, event, enabled state, condition count, action count, and latest update time.
- [x] Trigger logs viewer displays status, event, entity, started/completed times, errors, and JSON details.
- [x] Frontend validation keeps obvious incomplete builder rows from being submitted.
- [x] Existing behavior is not broken.
- [x] Documentation is updated if UI, route, or task status changes.
- [x] Relevant frontend tests/builds are run.

## Current Status

Completed for the current V4 slice. The `/triggers` workspace lets users select a form, view trigger summaries, create or edit V4 trigger definitions, save enabled/disabled state, and inspect execution logs. A minimal `GET /api/triggers/{triggerId}` endpoint was added so the UI can load full trigger details before editing.

## Out of Scope

- Scheduled triggers.
- Webhook listeners or webhook action execution.
- Retry queue/background worker.
- In-app notifications.
- Update-field and create-related-record actions.
- Workflow or approval engine.
- XYFlow.
- Backend trigger contract changes unless required by the UI.

## Tests

- Add frontend tests for trigger API request mapping, error handling, payload creation, and log formatting helpers.
- Run `npm test` and `npm run build` in `src/app`.
- Run backend harness/build if backend code is changed.

## Notes

- Implement only this task.
- Use the existing React, TypeScript, Tailwind, shared UI, and module registry patterns.
- Keep trigger builder helper logic outside React components where practical.
