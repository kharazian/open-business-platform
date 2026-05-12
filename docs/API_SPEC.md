# API Specification

This is a suggested REST-style API for the .NET Core backend.

Adapt endpoint names to the existing project style.

Status: draft. The current API skeleton only exposes `/health` and dashboard endpoints. Add these APIs task by task as product modules are implemented.

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
  "schema": {},
  "layout": {}
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
