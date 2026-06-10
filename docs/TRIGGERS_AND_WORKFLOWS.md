# Triggers and Workflows

Status: V4 task 001 backend trigger foundation, V4 task 002 trigger management UI, V4 task 003 update-field trigger action, V4 task 004 manual retry recovery, V4 task 005 in-app notification action, V4 task 006 notification inbox/read state, V4 task 007 notification badges/preferences, V4 task 008 create-record trigger actions, V4 task 009 automatic retry queues, V4 task 010 webhook action/retry policy/scheduled trigger closure, V5 task 001 backend workflow definition foundation, V5 task 002 workflow management UI, V5 task 003 record workflow transition execution, V5 task 004 approval inbox/notifications, V5 task 005 workflow transition action execution, V5 task 006 trigger-to-workflow starts, V5 task 007 optional XYFlow workflow builder, V6 trigger email record PDF attachments, V8 incoming webhook listener/import foundations, and V8 scheduled workflow starts are implemented. Advanced notification delivery, report/scheduled PDF actions, and custom code remain later tasks.

## Automation North Star

Triggers and workflows should share a small, safe action engine.

The platform needs to support:

- Event triggers, such as record created, record updated, field changed, and status changed.
- Scheduled triggers, such as daily, weekly, monthly, or one-time runs.
- Workflow starts from events, schedules, or manual actions.
- Actions such as create/update record, send email, attach generated record PDFs, call API/webhook, write notification, start workflow, or call another approved action.
- Execution logs for every trigger, workflow step, and action.

Custom code should not be the first automation primitive. Start with approved, typed actions that can be validated, permission-checked, audited, retried, and monitored. If custom code is added later, it should run in a restricted, auditable execution model.

## Difference Between Trigger and Workflow

A trigger is a small automation.

Example:

```txt
When record is created,
if department is HR,
send email to HR manager.
```

A workflow is a multi-step process.

Example:

```txt
Draft -> Submitted -> Manager Review -> Finance Review -> Approved
```

## Trigger Engine V4

The V4 task 001 backend foundation stores trigger definitions per form, stores trigger execution logs, exposes management APIs, and dispatches record events after the primary record transaction succeeds.

The V4 task 002 frontend workspace lets users manage form-scoped trigger definitions and review execution logs without adding new trigger semantics.

The V4 task 004 recovery slice lets form managers manually retry failed trigger execution logs. A retry replays the saved event input through the trigger's current action list and creates a new log linked to the failed source log.

The V4 task 009 reliability slice schedules failed trigger logs for automatic retry with a conservative three-attempt default. A hosted worker claims due logs, skips disabled triggers, replays the saved event input through the trigger's current action list, and creates fresh retry logs linked to the failed source log. V4 task 010 adds per-trigger retry policy editing, so managers can disable retries or adjust max attempts and delay within supported bounds.

The V4 task 005 action slice adds `send_notification`, which persists in-app notifications for selected active users and active group members. V4 task 006 adds the current-user notification inbox UI, unread count API, mark-one-read API, and mark-all-read API. V4 task 007 adds unread badges and current-user preferences; disabled in-app preferences cause trigger-created notifications to skip that user. V4 task 008 adds `create_record`, which creates one target-form record from literal values and source-record field references without recursive trigger dispatch. V4 task 010 adds `call_webhook`, which sends JSON webhook requests and lets non-success responses fail into the existing retry path. Push delivery, websockets, email fallback, and admin notification management remain future notification-module work.

The V4 task 010 schedule slice adds `schedule.once`, `schedule.daily`, `schedule.weekly`, and `schedule.monthly` events plus a hosted due-schedule worker. V8 task 007 makes daily, weekly, and monthly schedule contracts explicit with `interval`, `dayOfWeek`, and `dayOfMonth` metadata while preserving the original `startAt` behavior. V8 task 008 adds `scheduled_start_workflow` for explicit same-form record selections. Scheduled triggers support only safe non-record `send_email`/`call_webhook` actions plus this guarded workflow-start action.

V5 task 006 adds a typed `start_workflow` trigger action for current-record trigger events. The action validates an enabled, published, same-form workflow with a current published version. On execution, it starts the workflow only when the record has no active workflow, writes workflow history plus a `record_workflow_started_by_trigger` audit entry, and includes `workflowStartStatus` metadata in trigger logs. Duplicate active workflows are treated as a successful skip with reason `record_already_has_active_workflow`; invalid targets fail the trigger action and can use the existing retry path. To prevent recursive automation loops, trigger-started workflow status changes do not dispatch `status.changed` trigger events in this path.

V6 task 007 lets `send_email` trigger actions attach one generated record PDF from a published same-form record print template. Attachments are generated at execution time from the latest published template version and current record context, sent through the shared email sender, represented in trigger log result metadata, and audited as `print_template_pdf_email_attached`. Scheduled triggers, report PDFs, and multiple attachments remain later slices.

Trigger structure:

```txt
When event happens,
if conditions are true,
then execute actions.
```

## Trigger Events

- record.created
- record.updated
- field.changed
- status.changed
- record.assigned
- record.deleted later
- form.submitted later
- comment.added
- schedule.once
- schedule.daily
- schedule.weekly
- schedule.monthly
- webhook.received later

## Trigger Conditions

- Field equals value
- Field changed
- Status changed to value
- Department equals value
- Assigned user
- Assigned group
- Amount greater than value later
- User belongs to group later
- Date before/after later

## Trigger Actions

- Send email
- Change status
- Update field
- Assign user
- Assign group
- Add audit entry
- Send notification
- Create record
- Update related record
- Add comment
- Call API/webhook
- Attach generated record PDF to email
- Start workflow
- Start another trigger/action chain later

## Scheduled Trigger Model

Scheduled triggers run through the same trigger logging and retry foundation as event triggers.

Recommended fields:

- Trigger ID
- Name
- Enabled state
- Schedule type: once, daily, weekly, monthly, or cron-like later
- Time zone
- Start timestamp
- Interval, defaulting to 1
- Weekly day of week, 0 for Sunday through 6 for Saturday
- Monthly day of month, 1 through 31, clamped to shorter months at execution calculation time
- Next run time
- Last run time
- Conditions or guard rules
- Approved email or webhook action in V4
- Retry policy
- Owner and permission metadata

The scheduler executes due enabled trigger definitions and writes normal trigger logs with schedule metadata for due time, lock time, completion time, final status, next run when available, and skip reason when persisted schedule metadata cannot be used. V4/V8 scheduled triggers do not support record conditions or record-dependent actions.

Scheduled workflow starts use a separate `scheduled_start_workflow` action instead of reusing the current-record `start_workflow` action. The action must name an eligible enabled/published same-form workflow and a record selection mode: all records without active workflow, status equals, or field equals. Runtime selection ignores records that already have workflow state, writes the same workflow history/audit shape as trigger-started workflows, records per-record outcomes in the trigger log, and does not dispatch recursive status-changed trigger events.

## Action Engine

Actions are reusable units used by triggers and workflows.

Initial action types:

- Send email
- Attach generated record PDF to trigger email
- Create record
- Update record field/status
- Assign user or group
- Add audit/comment entry
- Call API/webhook
- Start workflow

Later action types:

- Generate report/scheduled PDFs
- Export data
- Run approved custom code in a restricted execution model

Every action should define typed inputs, validation rules, permission needs, audit/log output, and failure behavior.

## Trigger Logs

Every trigger execution should create a log:

- Trigger ID
- Event name
- Entity type
- Entity ID
- Status
- Input
- Result
- Error message
- Started at
- Completed at

## Workflow Engine V5

Workflow supports multi-step business processes.

Core concepts:

- State
- Transition
- Approval step
- Assignee
- Condition
- Action
- History

Workflows should use the same validation, permission, audit, notification, and action primitives as triggers. A workflow step should be able to call approved actions, and a trigger should be able to start a workflow.

The V5 task 001 backend foundation stores form-scoped workflow definitions, editable draft configs, immutable published workflow versions, and a workflow history table for future record transition execution. Management APIs support list, create, read, update, publish, enable, and disable operations with form `manage` permission checks and workflow mutation audit logs.

The first workflow slices began as table/form based authoring. The backend validates typed states, transitions, approval steps, assignee rules, and optional transition actions. The frontend `/workflows` workspace manages form-scoped workflow definitions with visual config editing, JSON-backed fallback editing, list, create, edit, publish, enable, and disable operations. V5 task 003 lets record detail users with scoped `change_status` access start an enabled published workflow and execute direct transitions. Direct transitions update the record workflow state, update `records.status` to the workflow state key, write workflow history/audit, and dispatch status-changed trigger events. V5 task 004 adds approval-gated transitions, current-user approval tasks, in-app notifications for assignees, any/all approval modes, rejection cancellation, approval history, and approval audit logs. V5 task 005 executes supported transition actions after direct or approval-completed transitions and represents each attempt in workflow history metadata. V5 task 006 lets trigger actions start eligible workflows without dispatching recursive status-changed triggers. V5 task 007 adds an optional workflow-only XYFlow visual builder over the same typed draft config; it does not persist graph layout metadata or change workflow execution semantics. V8 task 008 lets schedules start eligible workflows only through explicit same-form record selection rules. Advanced workflow notification delivery, incoming webhook listeners, report/scheduled PDF actions, and custom code remain future work.

Recommended V5 completion order:

1. Review and verify the whole system to finalize V5.

## Workflow States

Examples:

- Draft
- Submitted
- Manager Review
- Finance Review
- Approved
- Rejected
- Completed
- Cancelled

## Workflow Builder

Do not build workflow in V1.

In V5, XYFlow is used only for the visual workflow builder.

Do not use XYFlow for form layout.
