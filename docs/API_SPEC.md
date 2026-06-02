# API Specification

This is a REST-style API reference for the ASP.NET Core backend.

Status: evolving beyond V1. The V1 API baseline exposes health, development API explorer, cookie auth, dashboard summary, users, roles, role permissions, forms, published form rendering, record submission, record list/detail, record edit/delete, and per-form access management. V2 adds saved list report definition endpoints, runnable report execution, CSV export, real dashboard summary data, chart widget previews, and saved dashboard definitions. V3 adds groups, department management, scoped form permissions, report permissions, field rules, record assignment, and record status actions. Add later product APIs task by task as modules are implemented.

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

### Preview chart widget

`POST /api/forms/{formId}/chart-widgets/preview`

Requires authentication plus `menu.reports`, form `view`, form `manage`, or `forms.manage_all` access, and any V3 record scope attached to the user's form grant. If `reportId` is supplied, the user must also have report `view` access to that source report.

Runs a non-persisted chart widget config against permitted form records. `reportId` is optional; when supplied, the saved list report's filters are used as the chart source. V3 record scopes limit the record set, and hidden field rules remove fields from table columns, source report filters, and source report sorts. A chart request that directly references a hidden metric, group, or date field returns `403 Forbidden`.

Request:

```json
{
  "widgetType": "bar_chart",
  "metric": { "type": "count", "fieldId": null },
  "groupByFieldId": "status",
  "dateFieldId": null,
  "columns": [],
  "limit": 10,
  "reportId": null
}
```

Supported `widgetType` values are `number_card`, `bar_chart`, `date_trend`, `choice_breakdown`, and `table`. Supported metric types are `count`, `sum`, and `average`; sum and average require a numeric reportable field.

Response:

```json
{
  "formId": "00000000-0000-0000-0000-000000000000",
  "formName": "Employee information",
  "widgetType": "bar_chart",
  "metric": { "type": "count", "fieldId": null },
  "columns": [],
  "series": [
    { "key": "active", "label": "Active", "value": 10 }
  ],
  "rows": [],
  "totalCount": 10
}
```

Table widgets return `columns` and `rows` with display-ready cells instead of `series`. Returns `400` for invalid configs, `403` for failed permission checks, `404` when the form or source report is missing, and `409` when the form or report schema cannot be used for charting.

### Export list report CSV

`GET /api/forms/{formId}/reports/{reportId}/export.csv`

Requires authentication plus `menu.reports`, form `export` access, and report `export` access. Exports all rows allowed by the user's scoped `export` record access that match the saved report config and optional runtime search, not just the currently visible viewer page. CSV columns match the saved report's visible columns in saved order after hidden field rules are applied. Export requests write a `report_exported` audit log entry after the report is resolved.

Query parameters:

- `search` optional runtime search string, applied the same way as `GET /api/forms/{formId}/reports/{reportId}/run`

Response:

- `200 text/csv; charset=utf-8` with a safe report-based filename
- `403` for failed permission checks
- `404` when the form or report is missing
- `409` when the saved report config no longer matches the form schema

### Saved dashboards

`GET /api/dashboards`

Lists saved dashboard definitions. Requires authentication and `menu.dashboard`.

`GET /api/dashboards/{dashboardId}`

Returns a saved dashboard definition with `config` and `layout`. Requires authentication and `menu.dashboard`.

`POST /api/dashboards`

Creates a saved dashboard definition. Requires authentication and `reports.manage`.

`PUT /api/dashboards/{dashboardId}`

Updates a saved dashboard definition. Requires authentication and `reports.manage`.

Dashboard config stores widget definitions, and dashboard layout stores responsive width/order metadata. Supported widths are `small`, `medium`, `wide`, and `full`. Saved widgets reuse chart widget config values and are validated against source forms, source reports, fields, metrics, and widget types before save.

Saved dashboard definitions are persisted in the `dashboards` table added by the `DashboardDefinitions` EF Core migration. Workspace ownership is intentionally deferred to a later workspace module.

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
      "groups": [],
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
  "groupIds": [],
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
      "action": "view",
      "scope": "department"
    }
  ],
  "reportPermissions": [
    {
      "reportId": "00000000-0000-0000-0000-000000000000",
      "action": "export"
    }
  ],
  "fieldPermissions": [
    {
      "formId": "00000000-0000-0000-0000-000000000000",
      "fieldId": "salary",
      "access": "hidden"
    }
  ]
}
```

### Update role permissions

`PUT /api/roles/{roleId}/permissions`

Requires `roles.manage`.

Request shape matches the response from `GET /api/roles/{roleId}/permissions`. Supported form scopes are `all`, `own`, `department`, `managed_department`, `group`, and `assigned`. Supported report actions are `view`, `export`, and `manage`. Supported field access values are `hidden` and `read_only`.

### Groups

`GET /api/groups`

`POST /api/groups`

`GET /api/groups/{groupId}`

`PUT /api/groups/{groupId}`

All group endpoints require `users.manage`.

Create/update request:

```json
{
  "name": "Finance reviewers",
  "description": "Users who review finance records",
  "isActive": true,
  "concurrencyStamp": "stamp-for-update-only"
}
```

### Departments

`GET /api/departments`

`POST /api/departments`

`GET /api/departments/{departmentId}`

`PUT /api/departments/{departmentId}`

All department endpoints require `users.manage`.

Create/update request:

```json
{
  "name": "Finance",
  "parentDepartmentId": null,
  "managerUserId": "00000000-0000-0000-0000-000000000000",
  "isActive": true,
  "concurrencyStamp": "stamp-for-update-only"
}
```

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
  "ownerId": "22222222-2222-2222-2222-222222222222",
  "departmentId": null,
  "assignedToUserId": null,
  "assignedGroupId": null,
  "values": {
    "first_name": "John",
    "email": "john@example.com"
  },
  "concurrencyStamp": "record-stamp",
  "createdAt": "2026-05-21T00:00:00Z",
  "createdById": "22222222-2222-2222-2222-222222222222"
}
```

Record submission uses the form's current published version. The client sends only values; the backend checks submit access, validates values against the published schema, stores `records.form_version_id`, sets owner/department metadata from the current user, and writes a `record_created` audit entry. Draft and archived forms reject record submission.

### List records

`GET /api/forms/{formId}/records?page=1&pageSize=25&search=john`

Requires authentication plus form `view`, form `manage`, or `forms.manage_all` access. V3 scopes (`all`, `own`, `department`, `managed_department`, `group`, `assigned`) limit the returned rows. `page` is 1-based and `pageSize` is clamped by the backend.

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
      "ownerId": "22222222-2222-2222-2222-222222222222",
      "departmentId": null,
      "assignedToUserId": null,
      "assignedGroupId": null,
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

Search matches the record id, form version id, status, value keys, and value text for records on the requested form. Hidden field values are removed before records are returned.

### Get record detail

`GET /api/records/{recordId}`

Requires authentication plus form `view`, form `manage`, or `forms.manage_all` access for the record's form and a matching V3 record scope.

Response:

```json
{
  "id": "55555555-5555-5555-5555-555555555555",
  "formId": "11111111-1111-1111-1111-111111111111",
  "formVersionId": "44444444-4444-4444-4444-444444444444",
  "status": "active",
  "ownerId": "22222222-2222-2222-2222-222222222222",
  "departmentId": null,
  "assignedToUserId": null,
  "assignedGroupId": null,
  "values": {
    "first_name": "John",
    "email": "john@example.com"
  },
  "schema": {
    "schemaVersion": 1,
    "fields": [],
    "layout": { "pages": [] }
  },
  "readOnlyFieldIds": ["email"],
  "concurrencyStamp": "record-stamp",
  "createdAt": "2026-05-21T00:00:00Z",
  "createdById": "22222222-2222-2222-2222-222222222222",
  "updatedAt": null,
  "updatedById": null
}
```

Record detail returns the immutable schema from the stored `formVersionId` so values can be interpreted as they were at submission time. Hidden field values are omitted, and read-only field ids tell the client which returned values cannot be edited by the current user.

### Update record

`PUT /api/records/{recordId}`

Requires authentication plus form `edit`, form `manage`, or `forms.manage_all` access for the record's form and a matching V3 record scope. Updates validate values against the immutable schema for the stored `formVersionId`; they do not move the record to a newer form version.

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
  "ownerId": "22222222-2222-2222-2222-222222222222",
  "departmentId": null,
  "assignedToUserId": null,
  "assignedGroupId": null,
  "values": {
    "first_name": "Jane",
    "email": "jane@example.com"
  },
  "schema": {
    "schemaVersion": 1,
    "fields": [],
    "layout": { "pages": [] }
  },
  "readOnlyFieldIds": [],
  "concurrencyStamp": "new-record-stamp",
  "createdAt": "2026-05-21T00:00:00Z",
  "createdById": "22222222-2222-2222-2222-222222222222",
  "updatedAt": "2026-05-22T00:00:00Z",
  "updatedById": "22222222-2222-2222-2222-222222222222"
}
```

The backend returns `403` when the user attempts to modify hidden or read-only fields, returns `409` when the supplied `concurrencyStamp` is stale, and writes a `record_updated` audit entry on success.

### Delete record

`DELETE /api/records/{recordId}`

Requires authentication plus form `delete`, form `manage`, or `forms.manage_all` access for the record's form and a matching V3 record scope.

Response: `204 No Content`

Delete uses soft delete, marks the record status `deleted`, and writes a `record_deleted` audit entry.

### Assign record

`POST /api/records/{recordId}/assign`

Requires authentication plus form `assign`, form `manage`, or `forms.manage_all` access for the record's form and a matching V3 record scope.

Request:

```json
{
  "assignedToUserId": "33333333-3333-3333-3333-333333333333",
  "assignedGroupId": null,
  "concurrencyStamp": "record-stamp"
}
```

Response: `200 OK` with the refreshed record detail. The backend validates active assignees, returns `409` for stale concurrency stamps, and writes a `record_assigned` audit entry.

### Change record status

`POST /api/records/{recordId}/status`

Requires authentication plus form `change_status`, form `manage`, or `forms.manage_all` access for the record's form and a matching V3 record scope.

Request:

```json
{
  "status": "approved",
  "concurrencyStamp": "record-stamp"
}
```

Response: `200 OK` with the refreshed record detail. The backend validates the new status, returns `409` for stale concurrency stamps, and writes a `record_status_changed` audit entry.

## Reports V2/V3

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

Requires authentication plus `menu.reports`, form `view`, form `manage`, or `forms.manage_all` access, and report `view` access.

Runs a saved list report against non-deleted records for the form. The backend applies V3 record scopes, hidden field rules, saved report filters, optional runtime search, saved sort, and pagination before returning display-ready cells.

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

Returns `404` when the report does not exist for the form and `409` when the saved report config no longer matches the runnable form schema. Hidden fields are removed from report columns, filters, sorts, and row cells before execution results are returned.

### Export report CSV

`GET /api/forms/{formId}/reports/{reportId}/export.csv?search=Jane`

Requires authentication plus `menu.reports`, form `view`, form `manage`, or `forms.manage_all` access, and report `export` access.

Exports all permitted report rows matching the saved report config and optional runtime search. CSV columns match the visible saved report columns after hidden field rules are applied. Export requests write a `report_exported` audit entry after the report is resolved.

## Permissions V3

### Get permissions

`GET /api/permissions?resourceType=form&resourceId=form_1`

### Update permissions

`PUT /api/permissions`

## Triggers V4

Trigger APIs require authentication. All trigger management endpoints require form `manage` access for the target form, which is also granted by `forms.manage_all`.

Supported V4 task 001 events are `record.created`, `record.updated`, `field.changed`, `status.changed`, and `record.assigned`.

Supported condition types are:

- `field_equals`: `{ "type": "field_equals", "fieldId": "department", "value": "HR" }`
- `field_changed`: `{ "type": "field_changed", "fieldId": "email" }`
- `status_changed_to`: `{ "type": "status_changed_to", "status": "submitted" }`
- `department_equals`: `{ "type": "department_equals", "departmentId": "..." }`
- `assigned_to_user`: `{ "type": "assigned_to_user", "userId": "..." }`
- `assigned_to_group`: `{ "type": "assigned_to_group", "groupId": "..." }`

Supported action types are:

- `write_audit_entry`: writes an audit entry connected to the current record.
- `send_email`: sends one email per recipient through the configured email sender.
- `change_status`: changes the current record status without recursive trigger dispatch.
- `assign_record`: assigns the current record to one user or one group without recursive trigger dispatch.
- `update_field`: updates one field on the current record, validates the merged record values against the record's form version schema, writes a record audit entry, and does not recursively dispatch triggers.

### List triggers

`GET /api/forms/{formId}/triggers`

Requires form `manage` or `forms.manage_all` access. Response: `200 OK` with `{ "items": [...] }`.

### Create trigger

`POST /api/forms/{formId}/triggers`

Requires form `manage` or `forms.manage_all` access. The backend validates event names, condition payloads, action payloads, referenced form fields, active assignment targets, and email action requirements before saving. Creating a trigger writes a `trigger_created` audit entry.

Request:

```json
{
  "name": "Route HR submissions",
  "description": "Notify HR and move the record to review.",
  "eventName": "record.created",
  "conditions": {
    "mode": "all",
    "conditions": [
      { "type": "field_equals", "fieldId": "department", "value": "HR" }
    ]
  },
  "actions": [
    {
      "id": "audit-1",
      "type": "write_audit_entry",
      "message": "HR trigger matched."
    },
    {
      "id": "status-1",
      "type": "change_status",
      "status": "in_review"
    },
    {
      "id": "field-1",
      "type": "update_field",
      "fieldId": "email",
      "value": "jane@example.test"
    }
  ],
  "isEnabled": true
}
```

Response: `201 Created` with the saved trigger detail.

### Update trigger

`PUT /api/triggers/{triggerId}`

Requires form `manage` or `forms.manage_all` access for the trigger's form. The request shape matches create and also requires `concurrencyStamp`. Updating writes `trigger_updated`; changing from enabled to disabled also writes `trigger_disabled`.

### Get trigger

`GET /api/triggers/{triggerId}`

Requires form `manage` or `forms.manage_all` access for the trigger's form. Response: `200 OK` with the saved trigger detail, including conditions, actions, enabled state, and concurrency stamp. This is used by the trigger management UI before editing existing trigger definitions.

### Trigger logs

`GET /api/triggers/{triggerId}/logs`

Requires form `manage` or `forms.manage_all` access for the trigger's form. Response: `200 OK` with `{ "items": [...] }`, ordered newest first. Matching trigger executions write `success` or `failed` logs with input/result JSON and error message. Non-matching triggers do not write noisy skipped logs in this first slice.

Record submission dispatches `record.created`. Record edits dispatch `record.updated` and dispatch `field.changed` when values changed. Record status changes dispatch `status.changed`. Record assignment changes dispatch `record.assigned`. Trigger execution failures are logged and do not roll back the original record mutation.

## API Rules

- All APIs must validate input.
- All record APIs must check permissions.
- Hidden fields must not be returned to unauthorized users.
- Mutating APIs should write audit logs.
- Record create/update/status/assignment APIs dispatch V4 trigger events after the primary record transaction succeeds.
