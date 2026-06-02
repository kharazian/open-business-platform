# V4 Task Index

V4 starts the trigger engine and shared action foundation.

## Recommended Execution Order

1. `001-trigger-engine-foundation.md` - backend trigger definitions, event dispatch, starter actions, and logs. Completed for the backend foundation.
2. `002-trigger-management-ui.md` - frontend trigger list, builder, enable/disable editing, and logs viewer. Completed for the current UI slice.
3. `003-update-field-trigger-action.md` - safe current-record field update action. Completed for the current action slice.
4. `004-trigger-retry-recovery.md` - manual failed-log retry recovery. Completed for the current recovery slice.
5. `005-in-app-notification-trigger-action.md` - database-backed in-app notification action. Completed for the current action slice.
6. Additional actions and automatic retry behavior - later V4 task.

## Scope Rules

- Keep triggers separate from workflows.
- Use typed, approved actions; do not introduce custom code execution.
- Keep trigger definitions and execution logs on the backend.
- Enforce trigger management permissions on the backend.
- Dispatch from record events only after the primary record change succeeds.
- Do not implement scheduled triggers, webhook listeners, workflow approvals, or XYFlow in V4 task 001.
