# Data Model

## Database

Database: PostgreSQL

Status: proposed model. The current backend skeleton does not yet include EF Core or migrations.

Recommended approach:

- Relational tables for core entities.
- JSONB for dynamic schema/config/value structures.

## Core Tables

### users

Represents application users or maps to existing auth users.

Fields:

- id
- name
- email
- is_active
- created_at
- updated_at

### roles

Fields:

- id
- name
- description

### groups

Fields:

- id
- name
- description

### departments

Fields:

- id
- name
- parent_department_id
- manager_user_id

### user_roles

Fields:

- user_id
- role_id

### user_groups

Fields:

- user_id
- group_id

### user_departments

Fields:

- user_id
- department_id
- is_primary

## Forms

### forms

Fields:

- id
- name
- description
- status: draft, published, archived
- current_version_id
- created_by
- created_at
- updated_at

### form_versions

Fields:

- id
- form_id
- version_number
- schema_json JSONB
- layout_json JSONB
- validation_json JSONB
- published_by
- published_at
- created_at

The form version is immutable after publish.

## Records

### records

Fields:

- id
- form_id
- form_version_id
- status
- owner_id
- department_id
- values_json JSONB
- created_by
- updated_by
- created_at
- updated_at
- deleted_at nullable

Important indexes:

- form_id
- form_version_id
- status
- owner_id
- department_id
- created_by
- created_at

## Reports

### reports

Fields:

- id
- form_id
- name
- type: list, detail, summary, dashboard
- config_json JSONB
- created_by
- created_at
- updated_at

## Permissions

### permission_rules

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
- event_name
- conditions_json JSONB
- actions_json JSONB
- is_enabled
- created_by
- created_at
- updated_at

### trigger_logs

Fields:

- id
- trigger_id
- event_name
- entity_type
- entity_id
- status: success, failed, skipped
- input_json JSONB
- result_json JSONB
- error_message
- started_at
- completed_at

## Audit Logs

### audit_logs

Fields:

- id
- entity_type
- entity_id
- action
- user_id
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
- created_by
- created_at
- updated_at

## Example Form Schema JSON

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
  ]
}
```

## Example Layout JSON

```json
{
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
```
