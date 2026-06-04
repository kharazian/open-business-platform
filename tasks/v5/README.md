# V5 Task Index

V5 starts the workflow and approval engine. Workflows should reuse the existing form, record, permission, audit, notification, and action-engine foundations instead of creating a separate automation stack.

## Recommended Execution Order

1. `001-workflow-engine-foundation.md` - backend workflow definitions, states, transitions, approval step model, management APIs, and history foundation. Completed for the current backend foundation slice.
2. `002-workflow-management-ui.md` - table/form workflow management UI over the backend definition APIs. Completed for the current frontend management slice.
3. `003-record-workflow-transition-execution.md` - record workflow state, published workflow starts, direct transitions, history, audit, and record-detail controls. Completed for the current transition execution slice.
4. `004-approval-inbox-and-notifications.md` - current-user approval inbox APIs/UI, approval task persistence, approval notifications, any/all response modes, workflow history, and audit logs. Completed for the current approval execution slice.
5. Visual workflow builder - later task using XYFlow only after table/form-based workflow authoring works.

## Scope Rules

- Keep workflows separate from triggers while sharing approved action primitives where useful.
- Enforce workflow permissions on the backend.
- Keep workflow history auditable.
- Do not add XYFlow in the backend foundation task.
- Do not implement scheduled triggers, webhooks, PDF generation, or custom code execution in the early V5 workflow definition tasks.
