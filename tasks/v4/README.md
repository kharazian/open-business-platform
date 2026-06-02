# V4 Task Index

V4 starts the trigger engine and shared action foundation.

## Recommended Execution Order

1. `001-trigger-engine-foundation.md` - backend trigger definitions, event dispatch, starter actions, and logs.
2. Trigger management UI - later V4 task after backend contracts settle.
3. Additional actions and retry behavior - later V4 task.

## Scope Rules

- Keep triggers separate from workflows.
- Use typed, approved actions; do not introduce custom code execution.
- Keep trigger definitions and execution logs on the backend.
- Enforce trigger management permissions on the backend.
- Dispatch from record events only after the primary record change succeeds.
- Do not implement scheduled triggers, webhook listeners, workflow approvals, or XYFlow in V4 task 001.
