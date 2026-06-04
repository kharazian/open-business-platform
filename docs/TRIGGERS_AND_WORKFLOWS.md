# Triggers and Workflows

Status: V4 task 001 backend trigger foundation, V4 task 002 trigger management UI, V4 task 003 update-field trigger action, V4 task 004 manual retry recovery, V4 task 005 in-app notification action, V4 task 006 notification inbox/read state, V4 task 007 notification badges/preferences, V4 task 008 create-record trigger actions, V4 task 009 automatic retry queues, V4 task 010 webhook action/retry policy/scheduled trigger closure, V5 task 001 backend workflow definition foundation, and V5 task 002 workflow management UI are implemented. Incoming webhook listeners, record workflow execution, approval inboxes, workflow notifications, and XYFlow remain future tasks.

## Automation North Star

Triggers and workflows should share a small, safe action engine.

The platform needs to support:

- Event triggers, such as record created, record updated, field changed, and status changed.
- Scheduled triggers, such as daily, weekly, monthly, or one-time runs.
- Workflow starts from events, schedules, or manual actions.
- Actions such as create/update record, send email, call API/webhook, write notification, generate document later, start workflow, or call another approved action.
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

The V4 task 010 schedule slice adds `schedule.once`, `schedule.daily`, `schedule.weekly`, and `schedule.monthly` events plus a hosted due-schedule worker. In V4, scheduled triggers intentionally support only safe non-record actions: `send_email` and `call_webhook`. Record/workflow scheduled work remains future workflow/integration work.

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
- Generate PDF later
- Start workflow later
- Start another trigger/action chain later

## Scheduled Trigger Model

Scheduled triggers run through the same trigger logging and retry foundation as event triggers.

Recommended fields:

- Trigger ID
- Name
- Enabled state
- Schedule type: once, daily, weekly, monthly, or cron-like later
- Time zone
- Next run time
- Last run time
- Conditions or guard rules
- Approved email or webhook action in V4
- Retry policy
- Owner and permission metadata

The scheduler executes due enabled trigger definitions and writes normal trigger logs. V4 scheduled triggers do not support record conditions or record-dependent actions.

## Action Engine

Actions are reusable units used by triggers and workflows.

Initial action types:

- Send email
- Create record
- Update record field/status
- Assign user or group
- Add audit/comment entry
- Call API/webhook
- Start workflow

Later action types:

- Generate PDF
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

The first workflow slices are intentionally table/form based. The backend validates typed states, transitions, approval steps, assignee rules, and optional transition action placeholders. The frontend `/workflows` workspace manages form-scoped workflow definitions with JSON-backed config editing plus list, create, edit, publish, enable, and disable operations. These slices do not execute transitions, create approval inbox items, send workflow notifications, start workflows from triggers, or introduce XYFlow.

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

In V5, use XYFlow only for the visual workflow builder.

Do not use XYFlow for form layout.
