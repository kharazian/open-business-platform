# Triggers and Workflows

Status: V4 task 001 backend trigger foundation is implemented. Trigger builder UI, scheduled triggers, webhooks, retries, workflows, approvals, and XYFlow remain future tasks.

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
- schedule.daily later
- schedule.weekly later
- schedule.monthly later
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
- Assign user
- Assign group
- Add audit entry
- Send notification later
- Update field later
- Create record
- Update related record
- Add comment
- Call API/webhook
- Generate PDF later
- Start workflow later
- Start another trigger/action chain later

## Scheduled Trigger Model Later

Scheduled triggers should run through the same trigger engine as event triggers.

Recommended fields:

- Trigger ID
- Name
- Enabled state
- Schedule type: once, daily, weekly, monthly, or cron-like later
- Time zone
- Next run time
- Last run time
- Conditions or guard rules
- Action or workflow to start
- Retry policy
- Owner and permission metadata

The scheduler should enqueue approved trigger executions. The trigger engine should still evaluate permissions, conditions, action validation, and logs before doing work.

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
