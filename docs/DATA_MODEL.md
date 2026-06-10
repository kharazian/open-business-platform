# Data Model

## Database

Database: PostgreSQL

Status: V8 task 005 is complete for the current task list. The model includes core identity, form, record, report, dashboard, scoped permission, group, department, assignment, audit, trigger definition, trigger log, automatic trigger retry, trigger retry policy/schedule metadata, workflow definition/version/history, record workflow state, in-app notification, notification preference, print template, print template version, integration API key, integration log, incoming webhook listener, and record import job tables. The backend uses EF Core with Npgsql and keeps migrations in `src/api/Infrastructure/Persistence/Migrations`.

The current migrations include:

- `users`, `roles`, `user_roles`
- `password_reset_tokens`
- `integration_api_keys`
- `integration_logs`
- `incoming_webhook_listeners`
- `record_import_jobs`, `record_import_job_rows`
- `role_permissions`, `role_form_permissions`
- `groups`, `user_groups`
- `departments`, `user_departments`
- `role_report_permissions`, `role_field_permissions`
- `forms`, `form_versions`
- `records`
- `reports`
- `triggers`, `trigger_logs`
- `workflow_definitions`, `workflow_definition_versions`, `workflow_history`, `workflow_approval_tasks`
- `notifications`
- `notification_preferences`
- `print_templates`
- `print_template_versions`
- `audit_logs`

Recommended approach:

- Relational tables for core entities.
- JSONB for dynamic schema/config/value structures.
- Internal persisted entity IDs use PostgreSQL `uuid`/C# `Guid`.
- External authentication subject IDs are stored separately from the platform user primary key.

## Entity Foundation

The backend uses a small framework-lite entity foundation under `src/api/Domain/Common`.

Current base types:

- `Entity<TKey>`
- `AggregateRoot<TKey>`
- `CreationAuditedEntity<TKey>`
- `AuditedEntity<TKey>`
- `FullAuditedEntity<TKey>`
- `CreationAuditedAggregateRoot<TKey>`
- `AuditedAggregateRoot<TKey>`
- `FullAuditedAggregateRoot<TKey>`

Current capability interfaces include active status, concurrency stamp, soft delete, extra JSON properties, creation audit, modification audit, and deletion audit.

Generic CRUD primitives live under `src/api/Application/Common`, but generic CRUD should only be used for straightforward management resources. Form publishing, record submission, permission evaluation, trigger execution, workflow approval, and audit writing remain custom business flows.

## Core Tables

### users

Represents application users or maps to existing auth users.

Fields:

- id uuid
- name
- email
- is_active
- password_hash nullable
- password_updated_at nullable
- external_provider nullable
- external_user_id nullable
- concurrency_stamp
- extra_properties_json JSONB nullable
- created_at
- created_by_id nullable
- updated_at nullable
- updated_by_id nullable

### password_reset_tokens

Stores self-service password recovery tokens for persistent local users. Raw tokens are sent only in email reset links; PostgreSQL stores only a hash.

Fields:

- id uuid
- user_id uuid
- token_hash
- expires_at
- used_at nullable
- created_ip nullable
- created_at

Indexes:

- unique token_hash
- user_id
- expires_at

### integration_api_keys

Stores integration authentication keys. Raw API keys are returned only by create/rotate responses; PostgreSQL stores only `key_hash` plus a display/lookup `key_prefix`.

Fields:

- id uuid
- name
- integration_key stable normalized integration identity
- key_prefix
- key_hash
- scopes_json JSONB
- is_active
- last_used_at nullable
- last_used_ip nullable
- last_used_user_agent nullable
- revoked_at nullable
- revoked_by_id nullable
- concurrency_stamp
- extra_properties_json JSONB nullable
- created_at
- created_by_id nullable
- updated_at nullable
- updated_by_id nullable

Indexes:

- integration_key
- unique key_prefix
- unique key_hash
- is_active
- last_used_at
- revoked_at
- created_by_id
- revoked_by_id

Foreign keys:

- created_by_id -> users.id, set null on delete
- revoked_by_id -> users.id, set null on delete

### integration_logs

Stores observable inbound/outbound integration attempts. Metadata columns must store only sanitized request/response metadata, not raw request bodies, secret headers, raw API keys, or hidden field values.

Fields:

- id uuid
- direction: inbound, outbound
- integration_type: api, webhook, import, export
- integration_key stable normalized integration identity
- source_type
- source_id nullable
- target_entity_type nullable
- target_entity_id nullable
- status: pending, running, succeeded, failed, canceled
- attempt_count
- max_attempts
- is_retryable
- retry_next_attempt_at nullable
- retry_locked_at nullable
- retry_completed_at nullable
- retry_exhausted_at nullable
- retry_requested_at nullable
- retry_requested_by_id nullable
- request_metadata_json JSONB nullable
- response_metadata_json JSONB nullable
- error_code nullable
- error_message nullable
- started_at
- completed_at nullable
- concurrency_stamp
- extra_properties_json JSONB nullable
- created_at
- created_by_id nullable
- updated_at nullable
- updated_by_id nullable

Indexes:

- integration_key
- direction
- integration_type
- status
- source_type + source_id
- target_entity_type + target_entity_id
- retry_next_attempt_at
- created_at
- created_by_id
- retry_requested_by_id

Foreign keys:

- created_by_id -> users.id, set null on delete
- retry_requested_by_id -> users.id, set null on delete

### incoming_webhook_listeners

Stores named inbound webhook listener configurations. Listener mappings are typed JSONB definitions that map inbound payload source paths to fields on exactly one target form. Raw listener secrets are returned only on create/rotate; PostgreSQL stores only `secret_hash` plus a display/lookup `secret_prefix`.

Fields:

- id uuid
- name
- listener_key stable route key
- target_form_id
- action: create, upsert
- auth_mode: api_key, listener_secret
- secret_prefix nullable
- secret_hash nullable
- safe_lookup_field_id nullable
- mapping_json JSONB
- is_active
- concurrency_stamp
- extra_properties_json JSONB nullable
- created_at
- created_by_id nullable
- updated_at nullable
- updated_by_id nullable

Indexes:

- unique listener_key
- unique secret_prefix
- target_form_id
- is_active
- created_by_id

Foreign keys:

- target_form_id -> forms.id, restrict delete
- created_by_id -> users.id, set null on delete

### record_import_jobs

Stores CSV record import job metadata. Jobs target exactly one form and keep explicit CSV-header-to-target-field mappings in JSONB. Raw CSV content is not persisted; row-level results store only status, created record IDs, and sanitized validation errors.

Fields:

- id uuid
- form_id
- integration_key stable normalized integration identity
- file_name nullable safe display name
- status: pending, running, succeeded, completed_with_errors, failed
- total_rows
- succeeded_rows
- failed_rows
- started_at
- completed_at nullable
- mapping_json JSONB
- concurrency_stamp
- extra_properties_json JSONB nullable
- created_at
- created_by_id nullable
- updated_at nullable
- updated_by_id nullable

Indexes:

- form_id
- status
- created_at
- created_by_id

Foreign keys:

- form_id -> forms.id, restrict delete
- created_by_id -> users.id, set null on delete

### record_import_job_rows

Stores per-row import outcomes for queryable import diagnostics.

Fields:

- id uuid
- import_job_id
- row_number source CSV row number
- status: succeeded, failed
- record_id nullable
- errors_json JSONB nullable

Indexes:

- import_job_id
- status
- unique import_job_id + row_number
- record_id

Foreign keys:

- import_job_id -> record_import_jobs.id, cascade delete
- record_id -> records.id, set null on delete

### roles

Fields:

- id uuid
- name
- description
- is_active
- concurrency_stamp
- extra_properties_json JSONB nullable
- created_at
- created_by_id nullable
- updated_at nullable
- updated_by_id nullable

### departments

Fields:

- id uuid
- name
- parent_department_id nullable
- manager_user_id nullable
- is_active
- concurrency_stamp
- extra_properties_json JSONB nullable
- created_at
- created_by_id nullable
- updated_at nullable
- updated_by_id nullable

### user_roles

Fields:

- user_id uuid
- role_id uuid

### role_permissions

Stores global role grants such as menu visibility and platform actions.

Fields:

- id uuid
- role_id uuid
- permission

Indexes:

- role_id
- unique role_id + permission

### role_form_permissions

Stores role grants for specific forms.

Fields:

- id uuid
- role_id uuid
- form_id uuid
- action: submit, view, edit, delete, print, export, assign, change_status, manage
- scope: all, own, department, managed_department, group, assigned

Indexes:

- role_id
- form_id
- unique role_id + form_id + action

### groups

Fields:

- id uuid
- name
- description nullable
- is_active
- concurrency_stamp
- extra_properties_json JSONB nullable
- created_at
- created_by_id nullable
- updated_at nullable
- updated_by_id nullable

### user_groups

Fields:

- user_id
- group_id

### role_report_permissions

Fields:

- id uuid
- role_id uuid
- report_id uuid
- action: view, export, manage

### role_field_permissions

Fields:

- id uuid
- role_id uuid
- form_id uuid
- field_id
- access: hidden, read_only

### user_departments

Fields:

- user_id uuid
- department_id uuid
- is_primary

## Forms

### forms

Fields:

- id uuid
- name
- description
- status: draft, published, archived
- current_version_id nullable
- draft_schema_json JSONB nullable
- concurrency_stamp
- extra_properties_json JSONB nullable
- created_at
- created_by_id nullable
- updated_at nullable
- updated_by_id nullable
- is_deleted
- deleted_at nullable
- deleted_by_id nullable

`draft_schema_json` is the backend-owned builder draft. It may contain an incomplete V1 schema while a form is being edited. Publishing validates this draft strictly and copies it into an immutable `form_versions.schema_json` row.

### form_versions

Fields:

- id uuid
- form_id uuid
- version_number
- schema_json JSONB
- layout_json JSONB
- validation_json JSONB
- published_by_id nullable
- published_at
- created_at
- created_by_id nullable

The form version is immutable after publish.
The current in-code `FormSchemaDefinition` includes layout inside the schema object. Persistence can either store that canonical schema together in `schema_json` or split layout/validation into separate JSONB columns as long as API contracts remain consistent.

## Records

### records

Fields:

- id uuid
- form_id uuid
- form_version_id uuid
- status
- owner_id nullable
- department_id nullable
- assigned_to_user_id nullable
- assigned_group_id nullable
- workflow_definition_id nullable
- workflow_definition_version_id nullable
- workflow_state_key nullable
- values_json JSONB
- concurrency_stamp
- extra_properties_json JSONB nullable
- created_at
- created_by_id nullable
- updated_at nullable
- updated_by_id nullable
- is_deleted
- deleted_at nullable
- deleted_by_id nullable

Important indexes:

- form_id
- form_version_id
- status
- owner_id
- department_id
- assigned_to_user_id
- assigned_group_id
- workflow_definition_id
- workflow_definition_version_id
- workflow_state_key
- created_by_id
- created_at

V1 record submission stores values against the form's current published `form_versions.id`. The backend validates submitted values before insert and writes a `record_created` audit entry for the new record. V1 record list/detail queries return values with the stored `form_version_id`; detail responses also return the immutable published schema for that version. V1 record edits validate replacement values against the stored form version schema, check the record concurrency stamp, update `updated_at`/`updated_by_id`, and write `record_updated`. V1 record deletes use soft delete, set status `deleted`, populate deletion audit columns, and write `record_deleted`.

Migration `20260604130000_RecordWorkflowTransitions` adds nullable record workflow state columns. V5 task 003 sets these when an enabled published workflow starts on a record, keeps `workflow_definition_version_id` pointed at the immutable version used for execution, updates `status` to the current workflow state key, and writes workflow history/audit rows for starts and direct transitions.

## Reports

### reports

Fields:

- id uuid
- form_id uuid
- name
- type: list
- config_json JSONB
- concurrency_stamp
- extra_properties_json JSONB nullable
- created_at
- created_by_id nullable
- updated_at nullable
- updated_by_id nullable
- is_deleted
- deleted_at nullable
- deleted_by_id nullable

Indexes:

- form_id
- type
- created_by_id

The current V2 report definition stores list report configuration in `config_json`: selected columns, column order, custom labels, filters, and sort order. Report execution now runs saved list reports over real record data, and CSV export uses the same permission-checked report execution path. V3 report-level permission rows are implemented through `role_report_permissions`.

## Dashboards

### dashboards

Fields:

- id uuid
- name
- description nullable
- config_json JSONB
- layout_json JSONB
- concurrency_stamp
- extra_properties_json JSONB nullable
- created_at
- created_by_id nullable
- updated_at nullable
- updated_by_id nullable
- is_deleted
- deleted_at nullable
- deleted_by_id nullable

Indexes:

- created_by_id
- name

The current V2 dashboard definition stores saved dashboard widget config in `config_json` and responsive width/order metadata in `layout_json`. Saved widgets currently reuse chart widget config values. V7 task 001 adds read-only dashboard analytics execution contracts and does not require a dashboard schema migration.

V7 task 004 adds conservative dashboard visibility/default metadata in `extra_properties_json` without a schema migration:

```json
{
  "visibility": "workspace",
  "isDefault": false
}
```

Supported visibility values are `workspace` and `private`. Dashboards with missing metadata resolve to `workspace` visibility and `isDefault: false` so existing dashboards remain accessible under the old dashboard menu permission model. Private dashboards are visible to their creator and dashboard managers only. Only workspace-visible dashboards can be saved as the shared default.

## Permissions

### permission_rules later

Fields:

- id
- resource_type
- resource_id nullable
- actions_json JSONB
- subjects_json JSONB
- condition_json JSONB nullable
- effect: allow, deny
- priority
- created_at
- updated_at

## Triggers

### triggers

Fields:

- id
- form_id
- name
- description
- event_name
- conditions_json JSONB
- actions_json JSONB
- is_enabled
- auto_retry_enabled
- auto_retry_max_attempts
- auto_retry_delay_seconds
- schedule_json JSONB nullable
- schedule_next_run_at nullable
- schedule_last_run_at nullable
- concurrency_stamp
- extra_properties_json JSONB
- created_by_id
- created_at
- updated_by_id
- updated_at
- is_deleted
- deleted_at
- deleted_by_id

Indexes:

- form_id
- event_name
- is_enabled
- schedule_next_run_at

`conditions_json` stores an `all` group of typed condition objects. V4 task 001 supports field equality, field changed, status changed to, department equals, assigned user, and assigned group. `actions_json` stores ordered typed actions. V4 task 001 supports audit entry, send email, change status, and assign record. V4 task 003 adds current-record field updates through `update_field`. V4 task 005 adds `send_notification`. V4 task 008 adds `create_record`, which stores target form IDs and target field value mappings in `actions_json` and stores source trigger metadata on created records through `records.extra_properties_json`. V4 task 010 adds `call_webhook`, per-trigger retry policy columns, schedule metadata JSON, and due-schedule timestamps. V5 task 006 adds `start_workflow`, which stores the target workflow definition id in `actions_json` and writes trigger/action source metadata into workflow history and record audit logs at execution time. V6 task 007 adds optional `printTemplateId` metadata to `send_email` actions for generated record PDF attachments from a published same-form record print template. Trigger definitions are scoped to one form and are soft-deleted or disabled rather than physically removed while logs exist.

### trigger_logs

Fields:

- id
- trigger_id
- form_id
- event_name
- entity_type
- entity_id
- status: success, failed, skipped
- input_json JSONB
- result_json JSONB
- error_message
- started_at
- completed_at
- auto_retry_attempt_count
- auto_retry_max_attempts
- auto_retry_next_attempt_at nullable
- auto_retry_locked_at nullable
- auto_retry_completed_at nullable
- auto_retry_exhausted_at nullable
- auto_retry_disabled_at nullable
- created_at

Indexes:

- trigger_id
- form_id
- event_name
- entity_type + entity_id
- auto_retry_next_attempt_at
- created_at

Trigger logs persist matching trigger executions. The first V4 slice does not write skipped logs by default for non-matching triggers. Action failures write failed logs and do not roll back the original record change that dispatched the trigger event. V4 task 004 stores manual retry links in existing JSONB payloads through `input_json.retry.sourceLogId` and `result_json.retry.sourceLogId`, so no schema migration is required for manual failed-log retry recovery. V4 task 009 adds automatic retry metadata on failed source logs, with a conservative three-attempt default, a due-time index, lock/completed/exhausted/disabled timestamps, and fresh retry logs linked through the same retry JSON metadata as manual retries. V4 task 010 lets the source trigger's retry policy control the initial retry schedule and writes scheduled trigger executions with `entity_type = Schedule`.

## Workflows

Migrations: `20260604110000_WorkflowEngineFoundation`, `20260605192341_WorkflowSchemaAlignment`.

### workflow_definitions

Fields:

- id
- form_id
- name
- description nullable
- status: draft, published
- is_enabled
- has_unpublished_changes
- current_version_id nullable
- draft_config_json JSONB
- concurrency_stamp
- extra_properties_json JSONB nullable
- created_by_id
- created_at
- updated_by_id
- updated_at
- is_deleted
- deleted_at
- deleted_by_id

Indexes:

- form_id
- status
- is_enabled
- current_version_id

V5 task 001 adds this table for form-scoped workflow definition management. `draft_config_json` stores the editable typed workflow config: states, transitions, approval steps, assignee rules, and optional transition actions. Published versions are not mutated; edits after publishing set `has_unpublished_changes` until the draft is published again.
Migration `20260605192341_WorkflowSchemaAlignment` aligns the EF model snapshot and adds the missing default value for `workflow_definitions.is_deleted`.

### workflow_definition_versions

Fields:

- id
- workflow_definition_id
- form_id
- version_number
- config_json JSONB
- published_by_id nullable
- published_at nullable
- created_by_id
- created_at

Indexes:

- workflow_definition_id
- workflow_definition_id + version_number unique
- form_id

Workflow definition versions snapshot the validated draft config at publish time. Future record transition execution and workflow history should reference the version row used at the time so record history remains stable when workflow drafts change.

### workflow_history

Fields:

- id
- workflow_definition_id
- workflow_definition_version_id
- form_id
- record_id
- from_state_key nullable
- to_state_key
- transition_key nullable
- action
- actor_user_id nullable
- metadata_json JSONB nullable
- created_by_id
- created_at

Indexes:

- workflow_definition_id
- workflow_definition_version_id
- record_id
- form_id
- created_at

V5 task 003 writes this table for record workflow starts and direct transitions. V5 task 004 also writes approval request, approval response, rejection, cancellation, and approval-completed transition history. V5 task 005 represents transition action attempts in this table with `workflow_action_succeeded` and `workflow_action_failed` actions. Action attempt metadata stores action id, action type, status, error message, started time, completed time, and result details in `metadata_json`; no dedicated workflow action log table was added. V5 task 006 writes `workflow_started` history for trigger-started workflows with trigger id, trigger log id, action id, event name, and `startedByTrigger` metadata.

### workflow_approval_tasks

Migration: `20260604140000_WorkflowApprovalInbox`.

Fields:

- id
- approval_group_id
- workflow_definition_id
- workflow_definition_version_id
- form_id
- record_id
- approval_step_key
- approval_step_name
- mode
- transition_key
- transition_name
- from_state_key
- to_state_key
- status: pending, approved, rejected, canceled
- assigned_to_user_id
- requested_by_id nullable
- responded_by_id nullable
- responded_at nullable
- comment nullable
- created_by_id
- created_at
- updated_by_id
- updated_at

Indexes:

- approval_group_id
- assigned_to_user_id + status
- record_id + transition_key + status
- workflow_definition_id
- workflow_definition_version_id
- form_id
- requested_by_id
- responded_by_id

V5 task 004 adds this table for approval-gated workflow transitions. One row is created per resolved active approver so the current-user approval inbox can stay user-scoped. A shared `approval_group_id` links sibling tasks for the same requested transition; approvals and rejections update sibling rows according to `any` or `all` mode.

### V5 workflow persistence notes

V5 task 006 adds trigger-started workflow metadata in trigger logs and workflow history. V5 task 007 adds a frontend-only visual workflow builder and does not add graph layout tables, columns, or workflow config layout metadata.

### notifications

Migration: `20260602172402_InAppNotifications`.

Fields:

- id
- user_id
- title
- body
- source_type
- source_id nullable
- trigger_id nullable
- trigger_log_id nullable
- action_id nullable
- metadata_json JSONB nullable
- read_at nullable
- created_at

Indexes:

- user_id
- read_at
- created_at
- source_type + source_id

V4 task 005 adds this table for the `send_notification` trigger action. The action expands active groups to active users, deduplicates recipients, and stores trigger/action/source record metadata in each notification. V4 task 006 uses `read_at` for current-user inbox read state, unread counts, single-notification read marking, and mark-all-read APIs. V4 task 007 filters trigger-created notifications through current-user in-app preferences. Push delivery, websockets, email fallback, and admin notification management remain future work.

### notification_preferences

Migration: `20260603150110_NotificationPreferences`.

Fields:

- id
- user_id
- in_app_enabled
- show_unread_badge
- updated_at

Indexes:

- user_id unique
- updated_at

V4 task 007 adds this table for current-user notification preferences. Missing rows default to enabled in-app notifications and enabled unread badges. The trigger notification action skips users with `in_app_enabled = false`; `show_unread_badge` controls frontend navigation badges only.

## Audit Logs

### audit_logs

Fields:

- id uuid
- entity_type
- entity_id uuid
- action
- user_id nullable
- before_json JSONB nullable
- after_json JSONB nullable
- metadata_json JSONB nullable
- created_at

## Print Templates V6

### print_templates

Fields:

- id
- form_id
- report_id nullable
- name
- description nullable
- type: record, report
- config_json JSONB
- current_version_id nullable
- concurrency_stamp
- created_by_id
- created_at
- updated_by_id nullable
- updated_at
- deleted_by_id nullable
- deleted_at nullable
- is_deleted
- extra_properties_json JSONB nullable

Indexes:

- form_id
- report_id
- type
- created_by_id
- current_version_id

`config_json` stores schema version 1 header/footer settings, page setup options, repeated table header preference, ordered `fields`, `table`, and `signature` sections, per-section page-break preferences, optional field-based section conditions, optional logo URL text, selected field ids, and signature labels. Record template fields and conditions validate against the form schema; report template fields and conditions validate against reportable form/system fields. Browser print/save-as-PDF, server-side PDF downloads, and trigger email record PDF attachments are implemented for the V6 foundation.

### print_template_versions

Fields:

- id
- print_template_id
- form_id
- report_id nullable
- name
- description nullable
- type: record, report
- version_number
- config_json JSONB
- published_at
- published_by_id nullable
- created_at
- created_by_id nullable
- extra_properties_json JSONB nullable

Indexes:

- print_template_id
- form_id
- report_id
- published_at

Published print template versions are immutable snapshots used by browser rendering, server-side PDF downloads, and trigger email record PDF attachments.

## Example Form Schema JSON

This matches the current shared frontend/backend V1 schema shape.

```json
{
  "schemaVersion": 1,
  "fields": [
    {
      "id": "first_name",
      "type": "text",
      "label": "First Name",
      "required": true
    },
    {
      "id": "email",
      "type": "email",
      "label": "Email",
      "required": true
    }
  ],
  "layout": {
    "pages": [
      {
        "id": "page_1",
        "title": "Basic Info",
        "sections": [
          {
            "id": "section_1",
            "title": "Personal Information",
            "rows": [
              {
                "id": "row_1",
                "columns": [
                  {
                    "id": "col_1",
                    "span": {
                      "mobile": 12,
                      "tablet": 6,
                      "desktop": 6
                    },
                    "fields": ["first_name"]
                  },
                  {
                    "id": "col_2",
                    "span": {
                      "mobile": 12,
                      "tablet": 6,
                      "desktop": 6
                    },
                    "fields": ["email"]
                  }
                ]
              }
            ]
          }
        ]
      }
    ]
  }
}
```

## Current V1 Schema Validation Rules

The current frontend and backend validators enforce:

- `schemaVersion` must be `1`.
- At least one field is required.
- Field IDs must be present and unique.
- Field labels are required.
- Supported field types are `text`, `textarea`, `number`, `email`, `phone`, `date`, `select`, `checkbox`, and `radio`.
- `select` and `radio` fields require options with unique non-empty values.
- Layout requires at least one page, section, row, and column.
- Column spans must be integers from `1` to `12` for mobile, tablet, and desktop.
- Every schema field must be placed in the layout exactly once.

The current record value validator rejects unknown fields, enforces required values, checks basic types, validates email and `YYYY-MM-DD` date strings, and requires choice values to match defined options.
