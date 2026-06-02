# V4 Task 001: Trigger Engine Foundation

## Goal

Add the backend foundation for event-based triggers, safe starter actions, and trigger execution logs.

## Scope

This task implements backend trigger infrastructure only. The trigger builder UI is intentionally deferred to a later V4 task.

## Acceptance Criteria

- [ ] Trigger definitions are persisted per form.
- [ ] Trigger logs are persisted for matching executions.
- [ ] Backend APIs support listing, creating, updating, enabling, and disabling triggers.
- [ ] Backend APIs support listing trigger logs.
- [ ] Trigger management APIs require form `manage` or `forms.manage_all` access.
- [ ] Supported events include `record.created`, `record.updated`, `field.changed`, `status.changed`, and `record.assigned`.
- [ ] Supported conditions include field equality, field changed, status changed to, department equals, assigned user, and assigned group.
- [ ] Starter actions include audit entry, send email, change status, and assign record.
- [ ] Record submission dispatches `record.created`.
- [ ] Record edit dispatches `record.updated` and `field.changed` when values change.
- [ ] Record status changes dispatch `status.changed`.
- [ ] Record assignment dispatches `record.assigned`.
- [ ] Trigger actions suppress recursive trigger dispatch for this first slice.
- [ ] Trigger execution failures are logged and do not roll back the original record change.
- [ ] Backend validation rejects invalid trigger events, condition payloads, action payloads, and missing referenced fields.
- [ ] Documentation and verification are updated.

## Current Status

Planned. Use `docs/superpowers/specs/2026-06-02-v4-trigger-engine-foundation-design.md` as the approved design source.

## Out Of Scope

- Frontend trigger builder.
- Scheduled triggers.
- Webhook listeners.
- Workflow and approval engine.
- XYFlow.
- Arbitrary custom code execution.
- Retry queue/background worker.
