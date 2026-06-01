# API Specification

This is a REST-style API reference for the ASP.NET Core backend.

Status: evolving beyond V1. The V1 API baseline exposes health, development API explorer, cookie auth, dashboard summary, users, roles, role permissions, forms, published form rendering, record submission, record list/detail, record edit/delete, and per-form access management. Current V2 work adds saved list report definition endpoints, runnable report execution, and real dashboard summary data. Add later product APIs task by task as modules are implemented.

## Local API Explorer

In development, the backend exposes OpenAPI documentation for local browsing and ad-hoc testing:

- OpenAPI JSON: `GET /openapi/v1.json`
- Swagger UI: `/swagger`
- Scalar UI: `/scalar`

These endpoints are enabled only when the ASP.NET Core environment is `Development`.

## Current Implemented Endpoints

### Health

`GET /health`

Current response:

```json
{
  "status": "healthy",
  "service": "Open Business Platform API"
}
```

### Dashboard summary

`GET /api/dashboard/summary`

Requires authentication and `menu.dashboard`.

Current response:

```json
{
  "title": "Open Business Platform",
  "metrics": [
    { "key": "users", "label": "Users", "value": 4 },
    { "key": "forms", "label": "Forms", "value": 3 },
    { "key": "records", "label": "Records", "value": 10 },
    { "key": "reports", "label": "Reports", "value": 2 },
    { "key": "audit_logs", "label": "Audit logs", "value": 7 }
  ],
  "recentActivity": [
    {
      "id": "00000000-0000-0000-0000-000000000000",
      "event": "Record created",
      "actor": "Jane Cooper",
      "createdAt": "2026-05-22T12:00:00Z",
      "status": "Completed"
    }
  ]
}
```

Metrics are counted from PostgreSQL. Users count active users, forms/reports count non-deleted definitions, records count non-deleted active records, and audit logs count audit entries. Recent activity is sourced from the latest audit log rows.

## Shared V1 Form Schema Contract

The current shared schema contract is implemented in:

- Frontend: `src/app/src/features/forms`
- Backend: `src/api/Modules/Forms`

Supported V1 field types:

- `text`
- `textarea`
- `number`
- `email`
- `phone`
- `date`
- `select`
- `checkbox`
- `radio`

Canonical draft/version schema shape:

```json
{
  "schemaVersion": 1,
  "fields": [
    {
      "id": "first_name",
      "type": "text",
      "label": "First name",
      "required": true
    },
    {
      "id": "department",
      "type": "select",
      "label": "Department",
      "options": [
        { "id": "opt_finance", "label": "Finance", "value": "finance" }
      ]
    }
  ],
  "layout": {
    "pages": [
      {
        "id": "page_1",
        "title": "Employee",
        "sections": [
          {
            "id": "section_1",
            "rows": [
              {
                "id": "row_1",
                "columns": [
                  {
                    "id": "col_1",
                    "span": { "mobile": 12, "tablet": 6, "desktop": 6 },
                    "fields": ["first_name"]
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

Backend APIs must validate schema changes and submitted record values before persistence.

## Auth

The current V1 auth foundation uses cookie auth with local PostgreSQL users plus a server-only bootstrap admin configured through `.env` as a setup fallback. Self-service password recovery is available for active persistent users with local password hashes. The bootstrap admin fallback is recovered by changing its server-side environment configuration.

### Login

`POST /api/auth/login`

Request:

```json
{
  "email": "admin@company.test",
  "password": "change-me-before-use"
}
```

Response:

```json
{
  "user": {
    "id": "bootstrap-admin",
    "name": "Platform Admin",
    "email": "admin@company.test",
    "roles": ["Admin"],
    "permissions": [
      "menu.dashboard",
      "menu.forms",
      "menu.reports",
      "menu.users_access",
      "menu.settings",
      "menu.profile",
      "users.manage",
      "roles.manage",
      "forms.create",
      "forms.manage_all",
      "reports.manage"
    ]
  }
}
```

The API sets an HTTP-only auth cookie.

### Current user

`GET /api/auth/me`

Requires authentication.

### Logout

`POST /api/auth/logout`

Requires authentication and clears the auth cookie.

### Request password reset

`POST /api/auth/forgot-password`

Request:

```json
{
  "email": "jane@company.test"
}
```

Response:

```json
{
  "message": "If the email belongs to an active user, a password reset link will be sent."
}
```

The response is intentionally generic to avoid exposing whether an email belongs to a user. If the email belongs to an active persistent local user, the backend creates a short-lived password reset token, stores only the token hash, writes a `user_password_reset_requested` audit entry, and sends a reset email through configured SMTP. In development, if SMTP is not configured, the email body is logged by the API.

### Complete password reset

`POST /api/auth/reset-password`

Request:

```json
{
  "token": "raw-token-from-reset-link",
  "newPassword": "new-temporary-password-2"
}
```

Response: `204 No Content`

The backend rejects missing, expired, already-used, or unknown tokens with `400 Bad Request`, updates the user's password hash on success, marks the token as used, and writes a `user_password_reset_completed` audit entry.

Password recovery configuration can be supplied through environment variables:

- `PASSWORD_RESET_URL`
- `PASSWORD_RESET_TOKEN_MINUTES`
- `EMAIL_FROM_ADDRESS`
- `EMAIL_FROM_NAME`
- `SMTP_HOST`
- `SMTP_PORT`
- `SMTP_USERNAME`
- `SMTP_PASSWORD`
- `SMTP_USE_STARTTLS`

## Users & Access

The users and roles APIs require authentication and role-based backend authorization.

### List users

`GET /api/users`

Requires `users.manage`.

Response:

```json
{
  "items": [
    {
      "id": "00000000-0000-0000-0000-000000000000",
      "name": "Jane Cooper",
      "email": "jane@company.test",
      "isActive": true,
      "roles": [{ "id": "00000000-0000-0000-0000-000000000000", "name": "Admin" }],
      "departments": [],
      "concurrencyStamp": "stamp",
      "createdAt": "2026-05-19T00:00:00Z"
    }
  ]
}
```

### Create user

`POST /api/users`

Requires `users.manage`.

Request:

```json
{
  "name": "Jane Cooper",
  "email": "jane@company.test",
  "password": "temporary-password-1",
  "roleIds": ["00000000-0000-0000-0000-000000000000"],
  "departmentIds": [],
  "isActive": true
}
```

### Update user

`PUT /api/users/{userId}`

Requires `users.manage`.

### Reset user password

`POST /api/users/{userId}/reset-password`

Requires `users.manage`.

Request:

```json
{
  "newPassword": "new-temporary-password-2"
}
```

### List roles

`GET /api/roles`

Requires `roles.manage`.

### Create role

`POST /api/roles`

Requires `roles.manage`.

### Update role

`PUT /api/roles/{roleId}`

Requires `roles.manage`.

### Get role permissions

`GET /api/roles/{roleId}/permissions`

Requires `roles.manage`.

Response:

```json
{
  "roleId": "00000000-0000-0000-0000-000000000000",
  "permissions": ["menu.forms", "forms.create"],
  "formPermissions": [
    {
      "formId": "00000000-0000-0000-0000-000000000000",
      "action": "view"
    }
  ]
}
```

### Update role permissions

`PUT /api/roles/{roleId}/permissions`

Requires `roles.manage`.

### Form access options

`GET /api/forms/access-options`

Requires `roles.manage` or `forms.manage_all`. Returns form rows for the role-permissions matrix.

## Forms

`GET /api/forms`

Requires authentication plus `menu.forms`, `forms.create`, or `forms.manage_all`.

Response:

```json
{
  "items": [
    {
      "id": "00000000-0000-0000-0000-000000000000",
      "name": "Employee Form",
      "description": "Employee information",
      "status": "draft",
      "fieldCount": 0,
      "currentVersionId": null,
      "concurrencyStamp": "stamp",
      "createdAt": "2026-05-19T00:00:00Z",
      "createdById": null,
      "updatedAt": null,
      "updatedById": null
    }
  ]
}
```

`POST /api/forms`

Requires authentication plus `forms.create` or `forms.manage_all`.

Request:

```json
{
  "name": "Employee Form",
  "description": "Employee information"
}
```

Response: `201 Created` with a form summary. New forms are created as drafts.

### Get form draft

`GET /api/forms/{formId}`

Requires authentication plus form `manage` access or `forms.manage_all`.

Response:

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "name": "Employee Form",
  "description": "Employee information",
  "status": "draft",
  "fieldCount": 1,
  "currentVersionId": null,
  "draftSchema": {
    "schemaVersion": 1,
    "fields": [
      { "id": "first_name", "type": "text", "label": "First name", "required": true }
    ],
    "layout": {
      "pages": [
        {
          "id": "page_1",
          "sections": [
            {
              "id": "section_1",
              "rows": [
                {
                  "id": "row_1",
                  "columns": [
                    {
                      "id": "col_1",
                      "span": { "mobile": 12, "tablet": 12, "desktop": 12 },
                      "fields": ["first_name"]
                    }
                  ]
                }
              ]
            }
          ]
        }
      ]
    }
  },
  "concurrencyStamp": "stamp",
  "createdAt": "2026-05-19T00:00:00Z",
  "createdById": null,
  "updatedAt": null,
  "updatedById": null
}
```

`draftSchema` is nullable when a form exists but no backend draft has been saved yet. Browser `localStorage` may be used by the frontend only as a recovery cache.

### Update form draft

`PUT /api/forms/{formId}`

Requires authentication plus form `manage` access or `forms.manage_all`.

Request:

```json
{
  "name": "Employee Form",
  "description": "Employee information",
  "schema": {
    "schemaVersion": 1,
    "fields": [],
    "layout": {
      "pages": [
        {
          "id": "page_1",
          "sections": [{ "id": "section_1", "rows": [] }]
        }
      ]
    }
  }
}
```

Response: `200 OK` with the refreshed form detail. Draft saves update the editable draft metadata and schema together. `name` is trimmed and required; a blank or omitted `description` is stored as null. Draft saves allow incomplete builder schemas, but they still validate schema shape, field types, and layout references.

### Publish form

`POST /api/forms/{formId}/publish`

Requires authentication plus form `manage` access or `forms.manage_all`.

Publishes the saved backend draft. The request body is empty; the server does not publish browser-only draft state.

Response:

```json
{
  "form": {
    "id": "00000000-0000-0000-0000-000000000000",
    "name": "Employee Form",
    "description": "Employee information",
    "status": "published",
    "fieldCount": 1,
    "currentVersionId": "11111111-1111-1111-1111-111111111111",
    "draftSchema": {
      "schemaVersion": 1,
      "fields": [
        { "id": "first_name", "type": "text", "label": "First name", "required": true }
      ],
      "layout": {
        "pages": [
          {
            "id": "page_1",
            "sections": [
              {
                "id": "section_1",
                "rows": [
                  {
                    "id": "row_1",
                    "columns": [
                      {
                        "id": "col_1",
                        "span": { "mobile": 12, "tablet": 12, "desktop": 12 },
                        "fields": ["first_name"]
                      }
                    ]
                  }
                ]
              }
            ]
          }
        ]
      }
    },
    "concurrencyStamp": "stamp",
    "createdAt": "2026-05-19T00:00:00Z",
    "createdById": null,
    "updatedAt": "2026-05-21T00:00:00Z",
    "updatedById": null
  },
  "version": {
    "id": "11111111-1111-1111-1111-111111111111",
    "formId": "00000000-0000-0000-0000-000000000000",
    "versionNumber": 1,
    "schema": {
      "schemaVersion": 1,
      "fields": [
        { "id": "first_name", "type": "text", "label": "First name", "required": true }
      ],
      "layout": {
        "pages": [
          {
            "id": "page_1",
            "sections": [
              {
                "id": "section_1",
                "rows": [
                  {
                    "id": "row_1",
                    "columns": [
                      {
                        "id": "col_1",
                        "span": { "mobile": 12, "tablet": 12, "desktop": 12 },
                        "fields": ["first_name"]
                      }
                    ]
                  }
                ]
              }
            ]
          }
        ]
      }
    },
    "publishedById": null,
    "publishedAt": "2026-05-21T00:00:00Z"
  }
}
```

Publishing validates the backend draft with the strict schema validator, creates an immutable `form_versions` row, updates `forms.current_version_id`, marks the form `published`, and writes a `form_published` audit entry.

### Get published form for submission

`GET /api/forms/{formId}/published`

Requires authentication plus form `submit`, form `manage`, or `forms.manage_all` access. The response contains only the current published schema needed to render a submission form; it does not expose the editable draft schema.

Response:

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "name": "Employee Form",
  "description": "Employee information",
  "currentVersionId": "11111111-1111-1111-1111-111111111111",
  "currentVersionNumber": 1,
  "schema": {
    "schemaVersion": 1,
    "fields": [
      { "id": "first_name", "type": "text", "label": "First name", "required": true }
    ],
    "layout": {
      "pages": [
        {
          "id": "page_1",
          "sections": [
            {
              "id": "section_1",
              "rows": [
                {
                  "id": "row_1",
                  "columns": [
                    {
                      "id": "col_1",
                      "span": { "mobile": 12, "tablet": 12, "desktop": 12 },
                      "fields": ["first_name"]
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
}
```

Draft, archived, deleted, and invalid published forms do not render a submission schema.

## Planned Form Version Reading

### Get published form version

`GET /api/forms/{formId}/versions/{versionId}`

## Records

### Submit record

`POST /api/forms/{formId}/records`

Request:

```json
{
  "values": {
    "first_name": "John",
    "email": "john@example.com"
  }
}
```

Response:

```json
{
  "id": "55555555-5555-5555-5555-555555555555",
  "formId": "11111111-1111-1111-1111-111111111111",
  "formVersionId": "44444444-4444-4444-4444-444444444444",
  "status": "active",
  "values": {
    "first_name": "John",
    "email": "john@example.com"
  },
  "concurrencyStamp": "record-stamp",
  "createdAt": "2026-05-21T00:00:00Z",
  "createdById": "22222222-2222-2222-2222-222222222222"
}
```

Record submission uses the form's current published version. The client sends only values; the backend checks submit access, validates values against the published schema, stores `records.form_version_id`, and writes a `record_created` audit entry. Draft and archived forms reject record submission.

### List records

`GET /api/forms/{formId}/records?page=1&pageSize=25&search=john`

Requires authentication plus form `view`, form `manage`, or `forms.manage_all` access. `page` is 1-based and `pageSize` is clamped by the backend.

Response:

```json
{
  "totalCount": 1,
  "items": [
    {
      "id": "55555555-5555-5555-5555-555555555555",
      "formId": "11111111-1111-1111-1111-111111111111",
      "formVersionId": "44444444-4444-4444-4444-444444444444",
      "status": "active",
      "values": {
        "first_name": "John",
        "email": "john@example.com"
      },
      "createdAt": "2026-05-21T00:00:00Z",
      "createdById": "22222222-2222-2222-2222-222222222222"
    }
  ]
}
```

V1 search matches the record id, form version id, status, value keys, and value text for records on the requested form.

### Get record detail

`GET /api/records/{recordId}`

Requires authentication plus form `view`, form `manage`, or `forms.manage_all` access for the record's form.

Response:

```json
{
  "id": "55555555-5555-5555-5555-555555555555",
  "formId": "11111111-1111-1111-1111-111111111111",
  "formVersionId": "44444444-4444-4444-4444-444444444444",
  "status": "active",
  "values": {
    "first_name": "John",
    "email": "john@example.com"
  },
  "schema": {
    "schemaVersion": 1,
    "fields": [],
    "layout": { "pages": [] }
  },
  "concurrencyStamp": "record-stamp",
  "createdAt": "2026-05-21T00:00:00Z",
  "createdById": "22222222-2222-2222-2222-222222222222",
  "updatedAt": null,
  "updatedById": null
}
```

Record detail returns the immutable schema from the stored `formVersionId` so values can be interpreted as they were at submission time.

### Update record

`PUT /api/records/{recordId}`

Requires authentication plus form `edit`, form `manage`, or `forms.manage_all` access for the record's form. Updates validate values against the immutable schema for the stored `formVersionId`; they do not move the record to a newer form version.

Request:

```json
{
  "values": {
    "first_name": "Jane",
    "email": "jane@example.com"
  },
  "concurrencyStamp": "record-stamp"
}
```

Response:

```json
{
  "id": "55555555-5555-5555-5555-555555555555",
  "formId": "11111111-1111-1111-1111-111111111111",
  "formVersionId": "44444444-4444-4444-4444-444444444444",
  "status": "active",
  "values": {
    "first_name": "Jane",
    "email": "jane@example.com"
  },
  "schema": {
    "schemaVersion": 1,
    "fields": [],
    "layout": { "pages": [] }
  },
  "concurrencyStamp": "new-record-stamp",
  "createdAt": "2026-05-21T00:00:00Z",
  "createdById": "22222222-2222-2222-2222-222222222222",
  "updatedAt": "2026-05-22T00:00:00Z",
  "updatedById": "22222222-2222-2222-2222-222222222222"
}
```

The backend returns `409` when the supplied `concurrencyStamp` is stale and writes a `record_updated` audit entry on success.

### Delete record

`DELETE /api/records/{recordId}`

Requires authentication plus form `delete`, form `manage`, or `forms.manage_all` access for the record's form.

Response: `204 No Content`

Delete uses soft delete, marks the record status `deleted`, and writes a `record_deleted` audit entry.

## Reports V2

### List reports

`GET /api/forms/{formId}/reports`

Requires authentication plus `menu.reports` and form `view`, form `manage`, or `forms.manage_all` access.

Response:

```json
{
  "items": [
    {
      "id": "00000000-0000-0000-0000-000000000000",
      "formId": "00000000-0000-0000-0000-000000000000",
      "formName": "Employee Form",
      "name": "Employee directory",
      "type": "list",
      "columnCount": 3,
      "filterCount": 1,
      "sortCount": 1,
      "concurrencyStamp": "stamp",
      "createdAt": "2026-05-22T00:00:00Z",
      "createdById": null,
      "updatedAt": null,
      "updatedById": null
    }
  ]
}
```

### Create report

`POST /api/forms/{formId}/reports`

Requires authentication plus `reports.manage` and form `manage` or `forms.manage_all` access.

Request:

```json
{
  "name": "Employee directory",
  "config": {
    "schemaVersion": 1,
    "columns": [
      {
        "fieldId": "first_name",
        "label": "First name",
        "visible": true,
        "width": 180
      }
    ],
    "filters": [
      {
        "fieldId": "status",
        "operator": "equals",
        "value": "active"
      }
    ],
    "sort": [
      {
        "fieldId": "created_at",
        "direction": "desc"
      }
    ]
  }
}
```

Response: `201 Created` with the saved report detail. V2 validates report config against the form schema plus reportable system fields `status`, `created_at`, `created_by_id`, `updated_at`, `updated_by_id`, `owner_id`, and `department_id`. Supported filter operators are `equals`, `contains`, `is_empty`, and `is_not_empty`; supported sort directions are `asc` and `desc`. Creating a report writes a `report_created` audit entry.

### Run report

`GET /api/forms/{formId}/reports/{reportId}/run?page=1&pageSize=25&search=Jane`

Requires authentication plus `menu.reports` and form `view`, form `manage`, or `forms.manage_all` access.

Runs a saved list report against non-deleted records for the form. The backend applies the saved report filters, optional runtime search, saved sort, and pagination before returning display-ready cells.

Response:

```json
{
  "reportId": "00000000-0000-0000-0000-000000000000",
  "formId": "00000000-0000-0000-0000-000000000000",
  "reportName": "Employee directory",
  "formName": "Employee Form",
  "page": 1,
  "pageSize": 25,
  "totalCount": 1,
  "columns": [
    {
      "fieldId": "first_name",
      "label": "First name",
      "type": "text",
      "source": "form",
      "width": 180
    }
  ],
  "rows": [
    {
      "recordId": "00000000-0000-0000-0000-000000000000",
      "status": "active",
      "cells": {
        "first_name": {
          "value": "Jane",
          "displayValue": "Jane"
        }
      },
      "createdAt": "2026-05-22T00:00:00Z"
    }
  ]
}
```

Returns `404` when the report does not exist for the form and `409` when the saved report config no longer matches the runnable form schema.

### Export report CSV

`POST /api/reports/{reportId}/export/csv`

Not implemented yet.

## Permissions V3

### Get permissions

`GET /api/permissions?resourceType=form&resourceId=form_1`

### Update permissions

`PUT /api/permissions`

## Triggers V4

### List triggers

`GET /api/forms/{formId}/triggers`

### Create trigger

`POST /api/forms/{formId}/triggers`

### Update trigger

`PUT /api/triggers/{triggerId}`

### Trigger logs

`GET /api/triggers/{triggerId}/logs`

## API Rules

- All APIs must validate input.
- All record APIs must check permissions.
- Hidden fields must not be returned to unauthorized users.
- Mutating APIs should write audit logs.
- Record create/update APIs should eventually dispatch trigger events.
