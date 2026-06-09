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

- [ ] Schedule contracts are explicit and documented.
- [ ] Existing scheduled triggers continue to work.
- [ ] Due-time calculation is tested.
- [ ] Scheduled execution logs success and failure clearly.
- [ ] Unsafe schedule/action combinations are rejected.
- [ ] Documentation is updated.
- [ ] Tests are added where practical.
- [ ] Relevant build/test commands are run.

## Out of Scope

- Scheduled workflow starts.
- Custom code execution.
- Tenant-level scheduling policies.
- Arbitrary cron expressions unless explicitly justified.
