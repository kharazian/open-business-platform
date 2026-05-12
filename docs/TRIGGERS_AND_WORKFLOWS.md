# Triggers and Workflows

Status: planned. Do not implement triggers or workflows in V1 unless a task explicitly says so.

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

Trigger structure:

```txt
When event happens,
if conditions are true,
then execute actions.
```

## Trigger Events

- record.created
- record.updated
- record.deleted
- field.changed
- status.changed
- form.submitted
- record.assigned
- comment.added
- schedule.daily later
- webhook.received later

## Trigger Conditions

- Field equals value
- Field changed
- Status changed to value
- Amount greater than value
- Department equals value
- User belongs to group
- Date before/after

## Trigger Actions

- Send email
- Send notification
- Update field
- Change status
- Assign user
- Assign group
- Add comment
- Call webhook
- Generate PDF later
- Start workflow later

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
