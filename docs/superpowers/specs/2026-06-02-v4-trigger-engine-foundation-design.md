# V4 Trigger Engine Foundation Design

## Status

Approved direction on 2026-06-02.

This spec defines the first V4 slice: backend trigger, action, and execution-log foundations. It intentionally starts with backend contracts and event dispatch before adding a full trigger builder UI.

## Context

V1 through V3 now provide the data and security spine needed for automation:

- Published forms and immutable form versions.
- Record create, update, assignment, status change, soft-delete, and audit flows.
- Reportable field metadata and normalized system fields.
- Backend-enforced scoped record permissions.
- Basic field hidden/read-only rules.
- Existing notification email abstraction for password recovery.

V4 should make record changes automatable without introducing custom code, workflow approvals, scheduling, webhooks, or XYFlow in the first slice.

## Goals

- Add trigger definitions scoped to a form.
- Add trigger execution logs.
- Add a small typed action engine foundation.
- Dispatch trigger events from record submission and mutation paths after the primary record transaction succeeds.
- Support event triggers for record created, record updated, field changed, status changed, and record assigned.
- Validate trigger conditions and actions on the backend.
- Execute starter actions safely and auditably.
- Keep future workflow and scheduled-trigger work able to reuse the same action primitives.

## Non-Goals

- No frontend trigger builder in this first task.
- No XYFlow.
- No workflow states, approvals, or workflow history.
- No scheduled triggers.
- No inbound webhooks.
- No arbitrary code or scripting.
- No retry queue or background worker yet.
- No PDF/document generation.
- No cross-form joins or complex expression language.

## Recommended Approach

Build a backend module at `src/api/Modules/Triggers` with a small set of stable contracts:

- Trigger definition storage.
- Trigger condition validation and evaluation.
- Trigger action validation and execution.
- Trigger event dispatch from record services.
- Trigger execution logs.

The first implementation should execute synchronously after record mutations commit. That keeps behavior simple and testable. The log model should still record enough metadata to support queued retries later.

Actions should be registered by type, not interpreted as arbitrary code. Each action type owns its payload validation, permission needs, result shape, and failure behavior.

## Trigger Events

Supported in the first slice:

- `record.created`
- `record.updated`
- `field.changed`
- `status.changed`
- `record.assigned`

Later V4 or future automation slices can add:

- `record.deleted`
- `form.submitted` as an alias or richer event wrapper around record creation
- scheduled events
- inbound webhook events

## Condition Contract

Conditions are stored as JSONB and evaluated against a trigger event context. The first slice supports an `all` group of simple condition objects.

Example:

```json
{
  "mode": "all",
  "conditions": [
    {
      "type": "field_equals",
      "fieldId": "department",
      "value": "hr"
    },
    {
      "type": "status_changed_to",
      "status": "submitted"
    }
  ]
}
```

Supported condition types:

- `field_equals`
- `field_changed`
- `status_changed_to`
- `department_equals`
- `assigned_to_user`
- `assigned_to_group`

Empty conditions mean the trigger always matches the selected event. Hidden-field permissions do not remove data from trigger evaluation because triggers run server-side, but trigger management still requires privileged form management access.

## Action Contract

Actions are stored as JSONB in ordered execution order.

Example:

```json
[
  {
    "id": "action-1",
    "type": "send_email",
    "to": ["operations@example.com"],
    "subject": "Record submitted",
    "body": "A record was submitted for review."
  },
  {
    "id": "action-2",
    "type": "change_status",
    "status": "in_review"
  }
]
```

Starter action types:

- `write_audit_entry`: writes an audit log entry connected to the record.
- `send_email`: sends email through the existing notification email sender.
- `change_status`: changes the current record status.
- `assign_record`: assigns the current record to one user or one group.

Later V4 action types:

- `update_field`
- `create_record`
- `call_webhook`
- `send_notification`
- retry-aware email and webhook actions

Action execution should suppress recursive trigger dispatch in the first slice. Trigger chaining can be added later behind explicit limits and logs.

## Data Model

### triggers

Fields:

- `id uuid`
- `form_id uuid`
- `name`
- `description`
- `event_name`
- `conditions_json jsonb`
- `actions_json jsonb`
- `is_enabled`
- `concurrency_stamp`
- audit columns
- optional extra properties JSON

Indexes:

- `form_id`
- `event_name`
- `is_enabled`
- no unique name constraint in the first slice; duplicate names are allowed.

### trigger_logs

Fields:

- `id uuid`
- `trigger_id uuid`
- `form_id uuid`
- `event_name`
- `entity_type`
- `entity_id`
- `status`: `success`, `failed`, or `skipped`
- `input_json jsonb`
- `result_json jsonb`
- `error_message`
- `started_at`
- `completed_at`
- `created_at`

Indexes:

- `trigger_id`
- `form_id`
- `event_name`
- `entity_type + entity_id`
- `created_at`

Logs should remain even when an action fails. Trigger definition deletion should be soft-delete or disabled rather than physically deleting logs.

## Backend Design

Add a `Triggers` backend module:

- `TriggerDefinitionContracts.cs`
- `TriggerDefinitionService.cs`
- `TriggerDefinitionValidator.cs`
- `TriggerConditionEvaluator.cs`
- `TriggerActionRegistry.cs`
- `TriggerExecutionService.cs`
- `TriggerEventDispatcher.cs`
- `TriggersEndpoints.cs`
- `TriggersModule.cs`

Endpoints:

- `GET /api/forms/{formId}/triggers`
- `POST /api/forms/{formId}/triggers`
- `PUT /api/triggers/{triggerId}`
- `GET /api/triggers/{triggerId}/logs`

Authorization:

- Trigger list requires form `manage` or `forms.manage_all`.
- Trigger create/update requires form `manage` or `forms.manage_all`.
- Trigger logs require form `manage` or `forms.manage_all`.
- Action execution uses system context but should only execute actions that were validated at save time by an authorized manager.

Validation:

- Name is required.
- Event name must be supported.
- Conditions must match supported condition types and required payloads.
- Action list must not be empty.
- Action IDs must be unique.
- Action types must be supported.
- Referenced form fields must exist in the current published schema or draft schema used for trigger authoring.
- Assignment targets must be active users/groups when configured as literal IDs.
- Email actions require at least one recipient and a subject.

Execution:

1. Record service completes its primary transaction.
2. Record service dispatches a typed trigger event with before/after snapshots where available.
3. Dispatcher loads enabled triggers for the event and form.
4. Condition evaluator checks each trigger.
5. Execution service writes a `skipped` log for non-matching triggers only if diagnostics are enabled; default should avoid noisy skipped logs.
6. Matching triggers execute actions in order.
7. Each trigger execution writes one log with status, input, result, and error.
8. A failed action stops the remaining actions for that trigger and records a failed log.

## Event Payloads

Event context should include:

- event name
- form ID
- record ID
- actor user ID
- before record snapshot when available
- after record snapshot
- changed field IDs
- previous status and current status
- previous assignment and current assignment
- occurred-at timestamp

Snapshots should include record system fields and values needed for condition evaluation. They should not be returned to unauthorized clients; they are internal execution data.

## Frontend Design

The first V4 task does not add a trigger builder UI.

Frontend work in this slice should be limited to typed API helpers only if needed by tests or future UI preparation. A later task can add `src/app/src/features/triggers` with:

- trigger list
- compact trigger create/edit form
- condition controls
- action controls
- execution log viewer

The UI should stay operational and table/form based. XYFlow remains reserved for workflow and advanced automation diagrams later.

## Data Flow

Create/update trigger:

1. Admin opens trigger management in a later UI or uses the API in this first slice.
2. Backend checks form management permission.
3. Backend validates event, conditions, and actions against the selected form.
4. Backend saves JSONB config and writes audit logs.

Record event:

1. User creates or mutates a record through existing APIs.
2. Existing record permission checks run first.
3. Record transaction commits.
4. Trigger dispatcher receives the event context.
5. Matching triggers execute starter actions.
6. Execution logs and action audit entries are saved.

## Error Handling

- `400` for invalid trigger configuration.
- `403` for failed trigger management authorization.
- `404` for missing form, trigger, user, group, or referenced record.
- `409` when a trigger references schema data that is no longer valid.
- Trigger execution failures should not roll back the original record change in the first slice.
- Trigger action failures should be visible in trigger logs.

## Audit

Add audit events:

- `trigger_created`
- `trigger_updated`
- `trigger_disabled`
- `trigger_executed`
- `trigger_failed`

Starter actions that mutate records should continue using existing record audit actions where possible, with metadata linking back to the trigger log.

## Testing

Backend harness coverage should include:

- Trigger and trigger log entities map to the expected tables.
- JSONB columns are configured for conditions, actions, inputs, and results.
- Trigger validation rejects unsupported events, missing names, invalid conditions, invalid actions, duplicate action IDs, and missing form fields.
- Condition evaluator handles field equals, field changed, status changed, department, and assignment conditions.
- Dispatcher loads only enabled triggers for the form/event.
- Execution logs success and failure.
- Record submit/update/status/assignment paths dispatch the expected event context.
- Trigger management endpoints require form manage access.
- Trigger actions suppress recursive dispatch in the first slice.

Frontend tests are deferred until the trigger UI/API client task.

Build validation:

- `dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj`
- `dotnet build src/api/OpenBusinessPlatform.Api.csproj`
- `cd src/app && npm test`
- `cd src/app && npm run build`

## Documentation Updates

Implementation should update:

- `docs/API_SPEC.md`
- `docs/DATA_MODEL.md`
- `docs/TRIGGERS_AND_WORKFLOWS.md`
- `docs/MASTER_PRD_FOR_AI.md`
- `docs/ROADMAP.md`
- `tasks/v4/README.md`
- `tasks/v4/001-trigger-engine-foundation.md`

Document the migration because this task adds trigger tables.

## Acceptance Criteria

V4 task 001 is complete when:

- Trigger definitions can be created, listed, updated, enabled, and disabled through backend APIs.
- Trigger logs can be listed through backend APIs.
- Supported trigger events dispatch from record submission and mutation flows.
- Supported conditions evaluate against event context.
- Starter actions execute in order.
- Trigger execution logs success and failure outcomes.
- Backend authorization protects trigger management.
- No custom code, workflow, scheduler, webhook listener, or XYFlow is introduced.
- Documentation and tests are updated.
