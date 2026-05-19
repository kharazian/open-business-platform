# API Specification

This is a suggested REST-style API for the ASP.NET Core backend.

Adapt endpoint names to the existing project style.

Status: draft. The current API skeleton exposes only the health endpoint and the dashboard summary endpoint. Add product APIs task by task as modules are implemented.

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
    "roles": ["Admin"]
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

## Forms

### List forms

`GET /api/forms`

Response:

```json
{
  "items": [
    {
      "id": "form_1",
      "name": "Employee Form",
      "status": "published",
      "currentVersionId": "version_1"
    }
  ]
}
```

### Create form

`POST /api/forms`

Request:

```json
{
  "name": "Employee Form",
  "description": "Employee information"
}
```

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
