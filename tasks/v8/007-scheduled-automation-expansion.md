# V8 Task 007: Scheduled Automation Expansion

## Goal

Extend the existing scheduled trigger foundation with clearer daily, weekly, and monthly schedule contracts for safe automation.

## Context

Read:

- `docs/V8_START_HERE.md`
- `docs/TRIGGERS_AND_WORKFLOWS.md`
- `tasks/v4/010-webhooks-retry-policies-scheduled-triggers.md`
- `tasks/v8/002-integration-logs-and-retry-foundation.md`
- `AGENTS.md`

## Requirements

- Preserve existing scheduled trigger behavior.
- Add typed daily, weekly, and monthly schedule definitions if missing from current contracts.
- Keep scheduled actions limited to safe approved actions.
- Track due, locked, skipped, success, and failure metadata.
- Avoid ambiguous record context.
- Add integration or trigger logs for scheduled runs.

## Acceptance Criteria

- [x] Schedule contracts are explicit and documented.
- [x] Existing scheduled triggers continue to work.
- [x] Due-time calculation is tested.
- [x] Scheduled execution logs success and failure clearly.
- [x] Unsafe schedule/action combinations are rejected.
- [x] Documentation is updated.
- [x] Tests are added where practical.
- [x] Relevant build/test commands are run.

## Implementation Notes

- Expanded `TriggerScheduleDefinition` with explicit `interval`, weekly `dayOfWeek`, and monthly `dayOfMonth` metadata while preserving legacy `kind`/`timeZone`/`startAt` schedules.
- Updated due-time calculation for daily, weekly, and monthly schedules, including interval support and monthly day clamping for shorter months.
- Added scheduled trigger log metadata for due time, lock time, completion time, status, and skipped malformed persisted schedules.
- Kept scheduled actions limited to safe non-record `send_email` and `call_webhook` actions; record/workflow scheduled work remains out of scope.
- Updated frontend trigger schedule types and request normalization so UI-created weekly/monthly schedules send explicit metadata.

## Out of Scope

- Scheduled workflow starts.
- Custom code execution.
- Tenant-level scheduling policies.
- Arbitrary cron expressions unless explicitly justified.
