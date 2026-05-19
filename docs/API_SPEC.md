# API Specification

This is a suggested REST-style API for the ASP.NET Core backend.

Adapt endpoint names to the existing project style.

Status: draft. The current API skeleton exposes only the health endpoint and the dashboard summary endpoint. Add product APIs task by task as modules are implemented.

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

Current response:

```json
{
  "title": "Open Business Platform",
  "metrics": [
    { "label": "Users", "value": 0 },
    { "label": "Roles", "value": 0 },
    { "label": "Permissions", "value": 0 },
    { "label": "Audit logs", "value": 0 }
  ]
}
```

The dashboard endpoint is starter shell data. It is not connected to PostgreSQL yet.

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

The current V1 auth foundation uses a server-only bootstrap admin configured through `.env`.

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
      "menu.users_access",
      "users.manage",
      "roles.manage",
      "forms.manage_all"
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

### Forms

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

## Forms

### Future form editing

The following form editing and publishing endpoints are planned for later V1 tasks.

### Get form draft

`GET /api/forms/{formId}`

### Update form draft

`PUT /api/forms/{formId}`

Request:

```json
{
  "name": "Employee Form",
  "description": "Updated description",
  "schema": {
    "schemaVersion": 1,
    "fields": [],
    "layout": { "pages": [] }
  }
}
```

### Publish form

`POST /api/forms/{formId}/publish`

Creates immutable form version.

### Get published form version

`GET /api/forms/{formId}/versions/{versionId}`

## Records

### Submit record

`POST /api/forms/{formId}/records`

Request:

```json
{
  "formVersionId": "version_1",
  "values": {
    "first_name": "John",
    "email": "john@example.com"
  }
}
```

Response:

```json
{
  "id": "record_1",
  "formId": "form_1",
  "formVersionId": "version_1",
  "status": "submitted"
}
```

### List records

`GET /api/forms/{formId}/records?page=1&pageSize=25&search=john`

### Get record detail

`GET /api/records/{recordId}`

### Update record

`PUT /api/records/{recordId}`

### Delete record

`DELETE /api/records/{recordId}`

Use soft delete where possible.

## Reports V2

### List reports

`GET /api/forms/{formId}/reports`

### Create report

`POST /api/forms/{formId}/reports`

### Run report

`POST /api/reports/{reportId}/run`

### Export report CSV

`POST /api/reports/{reportId}/export/csv`

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
