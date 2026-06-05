# V5 Task Index

V5 starts the workflow and approval engine. Workflows should reuse the existing form, record, permission, audit, notification, and action-engine foundations instead of creating a separate automation stack.

## Recommended Execution Order

1. `001-workflow-engine-foundation.md` - backend workflow definitions, states, transitions, approval step model, management APIs, and history foundation. Completed for the current backend foundation slice.
2. `002-workflow-management-ui.md` - table/form workflow management UI over the backend definition APIs. Completed for the current frontend management slice.
3. `003-record-workflow-transition-execution.md` - record workflow state, published workflow starts, direct transitions, history, audit, and record-detail controls. Completed for the current transition execution slice.
4. `004-approval-inbox-and-notifications.md` - current-user approval inbox APIs/UI, approval task persistence, approval notifications, any/all response modes, workflow history, and audit logs. Completed for the current approval execution slice.
5. `005-workflow-transition-action-execution.md` - execute typed workflow transition actions after direct or approval-completed transitions, with deterministic logging, audit, validation, and failure behavior. Completed for the current action execution slice.
6. `006-trigger-to-workflow-starts.md` - let trigger actions start eligible published workflows on records without creating recursive automation loops. Next recommended V5 task.
7. `007-visual-workflow-builder.md` - optional XYFlow workflow builder over the existing typed workflow config after the table/form workflow stack is stable.

## Scope Rules

- Keep workflows separate from triggers while sharing approved action primitives where useful.
- Enforce workflow permissions on the backend.
- Keep workflow history auditable.
- Do not add XYFlow in the backend foundation task.
- Do not use XYFlow for form layout.
- Do not implement scheduled workflow starts, incoming webhook listeners, PDF generation, or custom code execution in V5 unless a task explicitly says so.
- Prefer typed action primitives already shared with triggers over new workflow-only action semantics.
