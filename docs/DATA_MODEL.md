# Data Model

## Database

Database: PostgreSQL

Status: V4 trigger foundation finalized for core identity, form, record, report, dashboard, scoped permission, group, department, assignment, audit, trigger definition, and trigger log tables. The backend uses EF Core with Npgsql and keeps migrations in `src/api/Infrastructure/Persistence/Migrations`.

The current migrations include:

- `users`, `roles`, `user_roles`
- `password_reset_tokens`
- `role_permissions`, `role_form_permissions`
- `groups`, `user_groups`
- `departments`, `user_departments`
- `role_report_permissions`, `role_field_permissions`
- `forms`, `form_versions`
- `records`
- `reports`
- `triggers`, `trigger_logs`
- `audit_logs`

Print templates remain target tables for later tasks.

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

### groups later

Fields:

- id
- name
- description

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

### user_groups later

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
- created_by_id
- created_at

V1 record submission stores values against the form's current published `form_versions.id`. The backend validates submitted values before insert and writes a `record_created` audit entry for the new record. V1 record list/detail queries return values with the stored `form_version_id`; detail responses also return the immutable published schema for that version. V1 record edits validate replacement values against the stored form version schema, check the record concurrency stamp, update `updated_at`/`updated_by_id`, and write `record_updated`. V1 record deletes use soft delete, set status `deleted`, populate deletion audit columns, and write `record_deleted`.

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

The current V2 report definition stores list report configuration in `config_json`: selected columns, column order, custom labels, filters, and sort order. Report execution now runs saved list reports over real record data, and CSV export uses the same permission-checked report execution path. Report-level permission rows are later V3 work.

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

`conditions_json` stores an `all` group of typed condition objects. V4 task 001 supports field equality, field changed, status changed to, department equals, assigned user, and assigned group. `actions_json` stores ordered typed actions. V4 task 001 supports audit entry, send email, change status, and assign record. V4 task 003 adds current-record field updates through `update_field`. Trigger definitions are scoped to one form and are soft-deleted or disabled rather than physically removed while logs exist.

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
- created_at

Indexes:

- trigger_id
- form_id
- event_name
- entity_type + entity_id
- created_at

Trigger logs persist matching trigger executions. The first V4 slice does not write skipped logs by default for non-matching triggers. Action failures write failed logs and do not roll back the original record change that dispatched the trigger event.

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

## Print Templates Later

### print_templates

Fields:

- id
- form_id
- report_id nullable
- name
- type: record, list
- config_json JSONB
- created_by_id
- created_at
- updated_at

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
