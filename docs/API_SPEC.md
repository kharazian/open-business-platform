# API Specification

This is a REST-style API reference for the ASP.NET Core backend.

Status: evolving beyond V1. The V1 API baseline exposes health, development API explorer, cookie auth, dashboard summary, users, roles, role permissions, forms, published form rendering, record submission, record list/detail, record edit/delete, and per-form access management. V2 adds saved list report definition endpoints, runnable report execution, CSV export, real dashboard summary data, chart widget previews, and saved dashboard definitions. V3 adds groups, department management, scoped form permissions, report permissions, field rules, record assignment, and record status actions. V4 adds trigger APIs, in-app notification creation, current-user notification inbox/read-state APIs, current-user notification preferences, related-record creation trigger actions, automatic failed-log retry queues, webhook call actions, user-authored retry policies, and scheduled trigger runs for safe actions. V5 adds backend workflow definition management, publish/version contracts, workflow history foundation tables, record workflow start/direct transition APIs, current-user workflow approval inbox APIs, transition action execution, and trigger-to-workflow start actions. V7 adds dashboard analytics execution for summary, breakdown, trend, and table widgets plus conservative saved dashboard visibility/default settings. V8 task 001 adds hashed integration API key management and API-key authentication plumbing without exposing record/report data. V8 task 002 adds integration log persistence, sanitized metadata, and explicit retry request metadata without background replay. V8 task 003 adds versioned API-key-authenticated record list/read/create endpoints that reuse existing form permissions, record scopes, validation, hidden-field filtering, audit logs, and integration logs. V8 task 004 adds incoming webhook listener management and receive endpoints with typed mappings, hashed listener secrets, backend permission checks, safe record create/upsert execution, and integration logs. V8 task 005 adds CSV record import jobs with explicit field mappings, persisted status, row-level results, existing record validation/permissions, audit logs, and integration logs. V8 task 006 adds external export jobs for permission-filtered form records and list reports with CSV/JSON artifacts, protected artifact content, audit logs, and integration logs. V8 task 007 expands scheduled automation contracts with explicit daily/weekly/monthly interval/day metadata, tested due-time calculation, and scheduled trigger log metadata. V8 task 008 adds explicit scheduled workflow-start actions over selected same-form records. V8 task 009 adds the `/integrations` operations UI for API keys, integration logs, and explicit retry requests. Add later product APIs task by task as modules are implemented.

## Local API Explorer

In development, the backend exposes OpenAPI documentation for local browsing and ad-hoc testing:

- OpenAPI JSON: `GET /openapi/v1.json`
- Swagger UI: `/swagger`
- Scalar UI: `/scalar`

These endpoints are enabled only when the ASP.NET Core environment is `Development`.

## Current Implemented Endpoints

## Current Implemented Frontend Routes

`/integrations`

Permission: `integrations.manage`.

The integrations operations workspace lets administrators create, revoke, and rotate integration API keys; view sanitized integration logs; filter logs by direction, type, status, source, and time; and request retry for eligible failed logs. Raw API key material is displayed only from create/rotate responses and is not recoverable from stored key metadata.

### Health

`GET /health`

Current response:

```json
{
  "status": "healthy",
  "service": "Open Business Platform API"
}
```

### Integration API keys

Integration API key management uses the normal cookie-authenticated admin surface. Every endpoint below requires authentication and `integrations.manage`.

The raw API key is returned only by create and rotate responses. List/get/revoke responses include `keyPrefix`, hash-free metadata, scopes, active/revoked state, last-used metadata, and audit timestamps, but never `keyHash` or the raw key.

Supported initial scopes are conservative and typed:

- `integrations.authenticate`
- `integrations.records.read`
- `integrations.records.create`
- `integrations.webhooks.receive`

`GET /api/integrations/api-keys`

Lists integration API keys:

```json
{
  "items": [
    {
      "id": "00000000-0000-0000-0000-000000000000",
      "name": "Payroll sync",
      "integrationKey": "payroll-sync",
      "keyPrefix": "obp_sk_exampleprefix",
      "scopes": ["integrations.authenticate"],
      "isActive": true,
      "lastUsedAt": null,
      "lastUsedIp": null,
      "lastUsedUserAgent": null,
      "revokedAt": null,
      "revokedById": null,
      "concurrencyStamp": "stamp",
      "createdAt": "2026-06-09T20:39:33Z",
      "createdById": null,
      "updatedAt": null,
      "updatedById": null
    }
  ]
}
```

`GET /api/integrations/api-keys/{apiKeyId}`

Returns one integration API key metadata record or `404`.

`POST /api/integrations/api-keys`

Creates a key for a stable integration identity.

Request:

```json
{
  "name": "Payroll sync",
  "integrationKey": "payroll-sync",
  "scopes": ["integrations.authenticate"],
  "isActive": true
}
```

Response: `201 Created`

```json
{
  "apiKey": {
    "id": "00000000-0000-0000-0000-000000000000",
    "name": "Payroll sync",
    "integrationKey": "payroll-sync",
    "keyPrefix": "obp_sk_exampleprefix",
    "scopes": ["integrations.authenticate"],
    "isActive": true,
    "lastUsedAt": null,
    "lastUsedIp": null,
    "lastUsedUserAgent": null,
    "revokedAt": null,
    "revokedById": null,
    "concurrencyStamp": "stamp",
    "createdAt": "2026-06-09T20:39:33Z",
    "createdById": null,
    "updatedAt": null,
    "updatedById": null
  },
  "rawKey": "obp_sk_exampleprefix.private-secret-segment"
}
```

`POST /api/integrations/api-keys/{apiKeyId}/revoke`

Revokes a key. Revoked keys cannot authenticate.

Request:

```json
{
  "reason": "No longer used",
  "concurrencyStamp": "stamp"
}
```

`POST /api/integrations/api-keys/{apiKeyId}/rotate`

Replaces the stored prefix/hash for an active, non-revoked key and returns the new raw key once.

Request:

```json
{
  "concurrencyStamp": "stamp"
}
```

Future integration endpoints can opt into the `IntegrationApiKey` authentication scheme. Requests may pass the key as `Authorization: Bearer <rawKey>` or `X-OBP-API-Key: <rawKey>`. A successful API-key authentication updates `lastUsedAt`, `lastUsedIp`, and `lastUsedUserAgent` only when the key is still active and non-revoked.

### Public/internal record API v1

The public/internal record API uses the `IntegrationApiKey` authentication scheme. Requests must pass a valid API key by `Authorization: Bearer <rawKey>` or `X-OBP-API-Key: <rawKey>`.

The API key must:

- be active and non-revoked,
- have `integrations.records.read` for list/read endpoints,
- have `integrations.records.create` for create endpoints,
- be linked to a platform user through its `createdById`.

Record permissions are evaluated through the linked user. That means existing backend form permissions, V3 record scopes, and hidden-field rules still apply. API keys without a linked platform user cannot access record data. Successful list/read/create operations write integration logs with source `PublicRecordApi` and do not store record values in log metadata. Creates also reuse the existing record submission audit log.

`GET /api/integration/v1/forms/{formId}/records`

Lists records for one form. Requires `integrations.records.read` and linked-user form `view` access. The existing V3 record scopes filter rows, and hidden fields are removed from returned `values`.

Query parameters:

- `page` default `1`
- `pageSize` default `25`, maximum `100`
- `search` optional

`GET /api/integration/v1/records/{recordId}`

Returns one record. Requires `integrations.records.read`, linked-user record `view` scope access, and hidden-field filtering.

`POST /api/integration/v1/forms/{formId}/records`

Creates a record for a published form. Requires `integrations.records.create` and linked-user form `submit` access. Values are validated through the existing backend form schema validator before save.

Request:

```json
{
  "values": {
    "email": "jane@example.test",
    "department": "Finance"
  }
}
```

Response: `201 Created`

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "formId": "00000000-0000-0000-0000-000000000000",
  "formVersionId": "00000000-0000-0000-0000-000000000000",
  "status": "active",
  "values": {
    "email": "jane@example.test",
    "department": "Finance"
  },
  "createdAt": "2026-06-09T21:20:00Z",
  "updatedAt": null
}
```

### Integration logs

Integration log management uses the normal cookie-authenticated admin surface. Every endpoint below requires authentication and `integrations.manage`.

Integration logs are a shared observability foundation for future inbound and outbound work. They track direction, type, stable integration identity, source, optional target entity, status, attempts, timestamps, sanitized metadata, and retry request metadata. Request and response metadata are intended for headers and operational metadata, not raw payloads; secret-like keys such as authorization headers, API keys, tokens, passwords, and secrets are redacted before persistence.

Supported directions:

- `inbound`
- `outbound`

Supported initial integration types:

- `api`
- `webhook`
- `import`
- `export`

Supported statuses:

- `pending`
- `running`
- `succeeded`
- `failed`
- `canceled`

`GET /api/integrations/logs`

Lists the latest integration logs, newest first.

`GET /api/integrations/logs/{logId}`

Returns one integration log or `404`.

Example response:

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "direction": "outbound",
  "integrationType": "webhook",
  "integrationKey": "payroll-sync",
  "sourceType": "Trigger",
  "sourceId": "00000000-0000-0000-0000-000000000000",
  "targetEntityType": "Record",
  "targetEntityId": "00000000-0000-0000-0000-000000000000",
  "status": "failed",
  "attemptCount": 1,
  "maxAttempts": 3,
  "isRetryable": true,
  "retryNextAttemptAt": "2026-06-09T21:02:28Z",
  "retryLockedAt": null,
  "retryCompletedAt": null,
  "retryExhaustedAt": null,
  "retryRequestedAt": null,
  "retryRequestedById": null,
  "requestMetadata": {
    "authorization": "[redacted]",
    "contentType": "application/json"
  },
  "responseMetadata": {
    "statusCode": 500
  },
  "errorCode": "remote_500",
  "errorMessage": "Remote service returned 500.",
  "startedAt": "2026-06-09T21:01:28Z",
  "completedAt": "2026-06-09T21:01:30Z",
  "concurrencyStamp": "stamp",
  "createdAt": "2026-06-09T21:01:30Z",
  "createdById": null,
  "updatedAt": null,
  "updatedById": null,
  "retryState": "pending"
}
```

`POST /api/integrations/logs/{logId}/retry-request`

Marks a retryable failed log for explicit retry handling and writes an audit log entry. This endpoint does not replay the integration action; future workers/UI can use the metadata to perform visible retry operations.

Request:

```json
{
  "concurrencyStamp": "stamp",
  "retryNextAttemptAt": "2026-06-09T21:05:00Z"
}
```

### Incoming webhook listeners

Incoming webhook listener management uses the normal cookie-authenticated admin surface. Every management endpoint below requires authentication and `integrations.manage`.

Listeners target exactly one form and store typed field mappings in JSONB. Listener secrets are stored only as hashes; the raw secret is returned only by create and rotate responses.

Supported listener actions:

- `create`
- `upsert`

Supported auth modes:

- `api_key`
- `listener_secret`

`GET /api/integrations/webhooks`

Lists configured incoming webhook listeners.

`GET /api/integrations/webhooks/{listenerId}`

Returns one listener or `404`.

`POST /api/integrations/webhooks`

Creates a listener and returns the raw listener secret once.

Request:

```json
{
  "name": "HR intake",
  "listenerKey": "hr-intake",
  "targetFormId": "00000000-0000-0000-0000-000000000000",
  "action": "create",
  "authMode": "api_key",
  "mapping": {
    "fieldMappings": [
      { "sourcePath": "person.email", "targetFieldId": "email", "required": true },
      { "sourcePath": "department", "targetFieldId": "department", "required": true }
    ]
  },
  "isActive": true
}
```

`PUT /api/integrations/webhooks/{listenerId}`

Updates listener metadata, target form, action, auth mode, active state, safe lookup field, and mapping. It does not return or change the raw listener secret.

`POST /api/integrations/webhooks/{listenerId}/rotate-secret`

Rotates the listener secret and returns the new raw secret once.

`POST /api/integration/v1/webhooks/{listenerKey}`

Receives an inbound webhook payload. The listener's configured auth mode determines authentication:

- `api_key`: pass `Authorization: Bearer <rawKey>` or `X-OBP-API-Key: <rawKey>`; the key must have `integrations.webhooks.receive` and be linked to a platform user.
- `listener_secret`: pass `X-OBP-Webhook-Secret: <rawSecret>`; the listener's creator is used for backend permission checks.

Payload values are mapped from configured source paths into target form field IDs. Create listeners submit a new record through existing record validation and permissions. Upsert listeners require `safeLookupFieldId`; zero matches create, one match updates through existing mutation permissions, and multiple matches return `409`.

Successful and failed receive attempts write inbound `webhook` integration logs with source `IncomingWebhookListener`. Logs store operational metadata such as listener key, action, auth mode, and mapped field count, not raw payload values.

Response:

```json
{
  "status": "succeeded",
  "recordId": "00000000-0000-0000-0000-000000000000",
  "integrationLogId": "00000000-0000-0000-0000-000000000000"
}
```

### Record import jobs

Record import job management uses the normal cookie-authenticated admin surface. Every endpoint below requires authentication and `integrations.manage`. Creating an import also requires the current user to have submit access to the target form.

This first slice supports CSV text only. Imports require explicit mappings from CSV headers to target form field IDs. Each row is submitted through the existing record creation service, so form versioning, backend validation, record audit logs, and trigger dispatch still apply. Row failures persist validation errors, not raw CSV values.

Supported job statuses:

- `pending`
- `running`
- `succeeded`
- `completed_with_errors`
- `failed`

Supported row statuses:

- `succeeded`
- `failed`

`GET /api/integrations/imports`

Lists recent record import jobs.

`GET /api/integrations/imports/{importJobId}`

Returns one import job with row-level results or `404`.

`POST /api/integrations/imports`

Creates and runs a CSV import job synchronously for the first foundation slice.

Request:

```json
{
  "formId": "00000000-0000-0000-0000-000000000000",
  "integrationKey": "hr-import",
  "fileName": "employees.csv",
  "csvContent": "Email Address,Department\njane@example.test,HR\nsam@example.test,Finance\n",
  "mapping": {
    "fieldMappings": [
      { "csvHeader": "Email Address", "targetFieldId": "email" },
      { "csvHeader": "Department", "targetFieldId": "department" }
    ]
  }
}
```

Response: `201 Created`

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "formId": "00000000-0000-0000-0000-000000000000",
  "integrationKey": "hr-import",
  "fileName": "employees.csv",
  "status": "succeeded",
  "totalRows": 2,
  "succeededRows": 2,
  "failedRows": 0,
  "mapping": {
    "fieldMappings": [
      { "csvHeader": "Email Address", "targetFieldId": "email" }
    ]
  },
  "rows": [
    {
      "id": "00000000-0000-0000-0000-000000000000",
      "rowNumber": 2,
      "status": "succeeded",
      "recordId": "00000000-0000-0000-0000-000000000000",
      "errors": []
    }
  ],
  "startedAt": "2026-06-10T13:38:07Z",
  "completedAt": "2026-06-10T13:38:08Z",
  "concurrencyStamp": "stamp",
  "createdAt": "2026-06-10T13:38:07Z",
  "createdById": "00000000-0000-0000-0000-000000000000",
  "updatedAt": "2026-06-10T13:38:08Z",
  "updatedById": "00000000-0000-0000-0000-000000000000"
}
```

Completed imports write inbound `import` integration logs with source `RecordImportJob`, row counts, status, and safe operational metadata.

### External export jobs

External export job management uses the normal cookie-authenticated admin surface. Every endpoint below requires authentication and `integrations.manage`. Creating an export also requires export access to the target form/report.

Exports support permission-filtered form records or saved list reports. Existing form permissions, report permissions, V3 record scopes, and hidden-field filtering are reused. Artifacts are stored as protected job content and metadata; no public download links are generated.

Supported source types:

- `form_records`
- `list_report`

Supported formats:

- `csv`
- `json`

Supported statuses:

- `pending`
- `running`
- `succeeded`
- `failed`

`GET /api/integrations/exports`

Lists recent export jobs.

`GET /api/integrations/exports/{exportJobId}`

Returns one export job, including protected artifact content, or `404`.

`POST /api/integrations/exports`

Creates and runs an export job synchronously for the first foundation slice.

Form record export request:

```json
{
  "sourceType": "form_records",
  "format": "csv",
  "integrationKey": "warehouse-export",
  "formId": "00000000-0000-0000-0000-000000000000",
  "search": "finance"
}
```

List report export request:

```json
{
  "sourceType": "list_report",
  "format": "json",
  "integrationKey": "warehouse-export",
  "formId": "00000000-0000-0000-0000-000000000000",
  "reportId": "00000000-0000-0000-0000-000000000000"
}
```

Response: `201 Created`

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "sourceType": "list_report",
  "format": "json",
  "integrationKey": "warehouse-export",
  "formId": "00000000-0000-0000-0000-000000000000",
  "reportId": "00000000-0000-0000-0000-000000000000",
  "status": "succeeded",
  "rowCount": 10,
  "artifactFileName": "employee-export.json",
  "artifactContentType": "application/json; charset=utf-8",
  "artifactSizeBytes": 2048,
  "artifactContent": "{...}",
  "artifactMetadata": {
    "fileName": "employee-export.json",
    "contentType": "application/json; charset=utf-8",
    "sizeBytes": 2048,
    "totalCount": 10,
    "columnCount": 4
  },
  "startedAt": "2026-06-10T14:37:06Z",
  "completedAt": "2026-06-10T14:37:07Z",
  "concurrencyStamp": "stamp",
  "createdAt": "2026-06-10T14:37:06Z",
  "createdById": "00000000-0000-0000-0000-000000000000",
  "updatedAt": "2026-06-10T14:37:07Z",
  "updatedById": "00000000-0000-0000-0000-000000000000"
}
```

Completed exports write outbound `export` integration logs with source `ExternalExportJob`, row counts, artifact metadata, and safe request metadata.

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

### Run dashboard analytics

`POST /api/dashboard/analytics/run`

Requires authentication, `menu.dashboard`, source form `view` access, and source report `view` access when `source.reportId` is supplied. Record rows are filtered through the user's V3 form record scope. Hidden fields cannot be directly selected and are removed from saved report source filters, sorts, and columns before execution.

Request:

```json
{
  "widgetType": "breakdown",
  "source": {
    "formId": "00000000-0000-0000-0000-000000000000",
    "reportId": null
  },
  "metric": {
    "type": "count",
    "fieldId": null
  },
  "groupByFieldId": "status",
  "dateFieldId": null,
  "columns": [],
  "limit": 10
}
```

Supported `widgetType` values are `summary`, `breakdown`, `trend`, and `table`. Supported metric types are `count`, `sum`, and `average`; sum and average require a numeric reportable field. Breakdown widgets require a status or choice-groupable field. Trend widgets require a date or datetime field.

Response:

```json
{
  "formId": "00000000-0000-0000-0000-000000000000",
  "formName": "Employee information",
  "reportId": null,
  "widgetType": "breakdown",
  "metric": { "type": "count", "fieldId": null },
  "series": [
    { "key": "active", "label": "Active", "value": 10 }
  ],
  "columns": [],
  "rows": [],
  "totalCount": 10
}
```

Table analytics return `columns` and `rows` with display-ready cells instead of `series`. Returns `400` for invalid analytics requests, `403` for failed menu/form/report/hidden-field checks, `404` when the source form or source report is missing, and `409` when the form or saved report schema cannot be used for analytics.

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

Lists saved dashboard definitions. Requires authentication and `menu.dashboard`. Workspace-visible dashboards are returned to dashboard viewers. Private dashboards are returned only to their creator or users with dashboard management permission. Default dashboards sort before other visible dashboards.

`GET /api/dashboards/{dashboardId}`

Returns a saved dashboard definition with `config`, `layout`, `visibility`, and `isDefault`. Requires authentication and `menu.dashboard` plus the same visibility rules used by the list endpoint.

`POST /api/dashboards`

Creates a saved dashboard definition. Requires authentication and `reports.manage`.

`PUT /api/dashboards/{dashboardId}`

Updates a saved dashboard definition. Requires authentication and `reports.manage`.

Dashboard config stores widget definitions, and dashboard layout stores responsive width/order metadata. Supported widths are `small`, `medium`, `wide`, and `full`. Saved widgets reuse chart widget config values and are validated against source forms, source reports, fields, metrics, and widget types before save.

Create and update requests may include optional dashboard settings:

```json
{
  "settings": {
    "visibility": "workspace",
    "isDefault": false
  }
}
```

Supported visibility values are `workspace` and `private`. Missing settings resolve to `workspace` and `isDefault: false`. Only workspace-visible dashboards can be saved as the shared default. Saving one dashboard as default clears the previous default.

Saved dashboard definitions are persisted in the `dashboards` table added by the `DashboardDefinitions` EF Core migration. V7 task 004 stores conservative visibility/default settings in `extra_properties_json`; it does not add a schema migration. Workspace ownership is intentionally deferred to a later workspace module.

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

## Printing V6

### List print templates

`GET /api/forms/{formId}/print-templates?type=record&reportId={reportId}`

Requires authentication plus form `print` or form `view` access. `type` is optional and supports `record` or `report`; `reportId` narrows report templates to one saved report. Report-scoped templates are returned only when the current user can view the saved report.

Response: `200 OK` with `{ "items": [...] }`. Items include `id`, `formId`, optional `reportId`, `name`, `description`, `type`, `sectionCount`, audit fields, `concurrencyStamp`, optional `currentVersionId`, optional `currentVersionNumber`, and optional `publishedAt`.

### Create print template

`POST /api/forms/{formId}/print-templates`

Requires authentication plus form `manage` or global `reports.manage`. Report-scoped templates also require manage access to the target saved report.

Request:

```json
{
  "name": "Employee record template",
  "description": "HR printable record",
  "type": "record",
  "reportId": null,
  "config": {
    "schemaVersion": 1,
    "type": "record",
    "layout": {
      "pageSize": "letter",
      "orientation": "portrait",
      "margin": "normal",
      "repeatTableHeaders": true
    },
    "header": {
      "title": "Employee record",
      "subtitle": "Record detail",
      "logoUrl": null,
      "showGeneratedAt": true
    },
    "sections": [
      {
        "id": "main",
        "kind": "fields",
        "title": "Fields",
        "fieldIds": ["first_name", "email"],
        "signatureLabels": [],
        "pagination": {
          "pageBreakBefore": false,
          "avoidBreakInside": true
        },
        "conditions": [
          {
            "fieldId": "department",
            "operator": "equals",
            "value": "Finance"
          }
        ]
      }
    ],
    "footer": {
      "text": "Open Business Platform"
    }
  }
}
```

Response: `201 Created` with the saved template detail. The backend validates name, record/report scope, config schema version, page size/orientation/margin, section kind, section ids, section condition operators, field ids against the form/reportable schema, and report ownership for report templates. Creates a `print_template_created` audit log entry.

### Get, update, delete print template

- `GET /api/print-templates/{templateId}` requires form `print` or form `view`; report templates also require report `view`.
- `PUT /api/print-templates/{templateId}` requires form `manage` or global `reports.manage`; report templates also require report `manage`, validates `concurrencyStamp`, and writes `print_template_updated`.
- `DELETE /api/print-templates/{templateId}` requires form `manage` or global `reports.manage`; report templates also require report `manage` and soft-deletes with `print_template_deleted`.

### Publish and use print template versions

- `POST /api/print-templates/{templateId}/versions` requires template manage access, validates `concurrencyStamp`, stores an immutable published version, updates the template's current version pointer, and writes `print_template_published`.
- `GET /api/print-templates/{templateId}/versions` requires template view access and lists immutable published versions newest first.
- `GET /api/print-template-versions/{versionId}` requires template view access and returns one immutable published version.
- `GET /api/print-template-versions/{versionId}/records/{recordId}.pdf` requires form print/view and record view access, renders a published record template version as `application/pdf`, and writes `print_template_pdf_generated`.
- `GET /api/print-template-versions/{versionId}/reports/{reportId}.pdf?page={page}&pageSize={pageSize}&search={search}` requires form print/view plus report view access, renders the permitted report execution page as `application/pdf`, and writes `print_template_pdf_generated`.

The V6 frontend uses templates for selected record-detail/report-viewer browser print output, server-side PDF downloads for published template versions, and trigger email record PDF attachments.

## Permissions V3

### Get permissions

`GET /api/permissions?resourceType=form&resourceId=form_1`

### Update permissions

`PUT /api/permissions`

## Notifications V4

Notification inbox APIs require authentication and always use the authenticated current user id. They do not expose cross-user notification browsing or mutation. Bootstrap/setup identities that do not map to a persisted user `Guid` receive an empty inbox, unread count, and default preferences.

### List current-user notifications

`GET /api/notifications`

Response: `200 OK` with `{ "items": [...] }`, ordered newest first.

Notification item shape:

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "title": "HR record needs review",
  "body": "Open the record and review the submitted details.",
  "sourceType": "Record",
  "sourceId": "00000000-0000-0000-0000-000000000000",
  "triggerId": "00000000-0000-0000-0000-000000000000",
  "triggerLogId": "00000000-0000-0000-0000-000000000000",
  "actionId": "notify-1",
  "metadata": { "recordId": "00000000-0000-0000-0000-000000000000" },
  "readAt": null,
  "createdAt": "2026-06-02T17:00:00Z"
}
```

### Get unread count

`GET /api/notifications/unread-count`

Response: `200 OK`.

```json
{
  "unreadCount": 3
}
```

### Mark one notification read

`POST /api/notifications/{notificationId}/read`

Marks the current user's notification as read and returns the updated notification. Missing notifications, including notifications owned by another user, return `404 Not Found`.

### Mark all notifications read

`POST /api/notifications/read-all`

Marks all unread notifications for the current user as read and returns the remaining unread count.

### Get notification preferences

`GET /api/notifications/preferences`

Returns the current user's notification preferences. Users without a saved preference row receive safe defaults.

```json
{
  "inAppEnabled": true,
  "showUnreadBadge": true,
  "updatedAt": null
}
```

### Update notification preferences

`PUT /api/notifications/preferences`

Request:

```json
{
  "inAppEnabled": false,
  "showUnreadBadge": true
}
```

Response: `200 OK` with the saved preference DTO. Missing current-user rows are created on update. Trigger-created in-app notifications skip users with `inAppEnabled` set to `false`; `showUnreadBadge` controls frontend navigation badges.

## Triggers V4

Trigger APIs require authentication. All trigger management endpoints require form `manage` access for the target form, which is also granted by `forms.manage_all`.

Supported V4 events are `record.created`, `record.updated`, `field.changed`, `status.changed`, `record.assigned`, `schedule.once`, `schedule.daily`, `schedule.weekly`, and `schedule.monthly`.

Supported condition types are:

- `field_equals`: `{ "type": "field_equals", "fieldId": "department", "value": "HR" }`
- `field_changed`: `{ "type": "field_changed", "fieldId": "email" }`
- `status_changed_to`: `{ "type": "status_changed_to", "status": "submitted" }`
- `department_equals`: `{ "type": "department_equals", "departmentId": "..." }`
- `assigned_to_user`: `{ "type": "assigned_to_user", "userId": "..." }`
- `assigned_to_group`: `{ "type": "assigned_to_group", "groupId": "..." }`

Supported action types are:

- `write_audit_entry`: writes an audit entry connected to the current record.
- `send_email`: sends one email per recipient through the configured email sender. Optional `printTemplateId` attaches a generated record PDF from a published same-form record print template when the trigger has a current record context.
- `change_status`: changes the current record status without recursive trigger dispatch.
- `assign_record`: assigns the current record to one user or one group without recursive trigger dispatch.
- `update_field`: updates one field on the current record, validates the merged record values against the record's form version schema, writes a record audit entry, and does not recursively dispatch triggers.
- `send_notification`: creates in-app notifications for selected active users and active group members.
- `create_record`: creates one record in another published target form using typed literal values or source-field references, validates the target record values, writes source trigger metadata on the created record, and does not recursively dispatch triggers.
- `call_webhook`: sends an HTTP JSON request to an absolute `http` or `https` URL. Non-success responses fail the trigger action and can be retried.
- `start_workflow`: starts an enabled published same-form workflow with a current version on the current record when no workflow is already active.
- `scheduled_start_workflow`: starts an enabled published same-form workflow for explicitly selected same-form records that do not already have active workflow state.

Scheduled trigger events require `schedule` metadata and currently support `send_email`, `call_webhook`, and `scheduled_start_workflow` actions. Scheduled `send_email` actions cannot attach record PDFs because no current record exists. `start_workflow` requires an event record context and is intentionally not supported for scheduled triggers.

Schedule metadata shape:

```json
{
  "kind": "weekly",
  "timeZone": "Etc/UTC",
  "startAt": "2026-06-01T09:30:00Z",
  "interval": 2,
  "dayOfWeek": 1,
  "dayOfMonth": null
}
```

`interval` defaults to `1` and must be between `1` and `366`. Weekly `dayOfWeek` is optional and uses `0` for Sunday through `6` for Saturday. Monthly `dayOfMonth` is optional, must be `1` through `31`, and clamps to the last valid day in shorter months when calculating the next due time. Existing schedules that only contain `kind`, `timeZone`, and `startAt` remain valid.

`start_workflow` action payload:

```json
{
  "id": "workflow-1",
  "type": "start_workflow",
  "workflowDefinitionId": "11111111-1111-1111-1111-111111111111"
}
```

The backend validates the referenced workflow before saving the trigger. It must exist, be enabled, be published, belong to the trigger form, and point to a current published version. Execution updates `records.workflow_definition_id`, `records.workflow_definition_version_id`, `records.workflow_state_key`, and `records.status` to the workflow initial state key; writes `workflow_started` history with trigger id, trigger log id, action id, and event name metadata; and writes a `record_workflow_started_by_trigger` audit entry. This trigger path does not dispatch a follow-up `status.changed` trigger event, even though the record status changes, to prevent recursive automation loops. If the record already has active workflow state, the action succeeds as a skip with `workflowStartStatus: "skipped"` and `reason: "record_already_has_active_workflow"`. Invalid execution-time targets fail the action; the trigger log result includes failed action metadata and normal retry handling applies.

`scheduled_start_workflow` action payload:

```json
{
  "id": "scheduled-workflow-1",
  "type": "scheduled_start_workflow",
  "workflowDefinitionId": "11111111-1111-1111-1111-111111111111",
  "recordSelection": {
    "mode": "status_equals",
    "status": "submitted",
    "maxRecords": 100
  }
}
```

Supported `recordSelection.mode` values are `all_records_without_active_workflow`, `status_equals`, and `field_equals`. Field selection also requires `fieldId` and `value`. `maxRecords` must be between `1` and `500`. The scheduled path validates the same workflow eligibility as `start_workflow`, only selects records from the trigger form, ignores records that already have active workflow state, writes workflow history/audit entries, and includes per-record results in the trigger log. It does not dispatch recursive `status.changed` trigger events.

### List triggers

`GET /api/forms/{formId}/triggers`

Requires form `manage` or `forms.manage_all` access. Response: `200 OK` with `{ "items": [...] }`.

### Create trigger

`POST /api/forms/{formId}/triggers`

Requires form `manage` or `forms.manage_all` access. The backend validates event names, condition payloads, action payloads, referenced source form fields, published target forms for `create_record`, target record values, enabled/published same-form workflow targets for `start_workflow`, active assignment targets, active notification recipients, and email/notification action requirements before saving. Creating a trigger writes a `trigger_created` audit entry.

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
    },
    {
      "id": "notify-1",
      "type": "send_notification",
      "title": "HR record needs review",
      "body": "Open the record and review the submitted details.",
      "recipientUserIds": ["..."],
      "recipientGroupIds": ["..."]
    },
    {
      "id": "create-1",
      "type": "create_record",
      "targetFormId": "...",
      "values": {
        "email": { "sourceFieldId": "email" },
        "department": { "literal": "HR" }
      }
    },
    {
      "id": "webhook-1",
      "type": "call_webhook",
      "webhookUrl": "https://hooks.example.test/records",
      "webhookMethod": "POST",
      "webhookHeaders": {
        "X-Source": "open-business-platform"
      }
    },
    {
      "id": "workflow-1",
      "type": "start_workflow",
      "workflowDefinitionId": "..."
    }
  ],
  "retryPolicy": {
    "isEnabled": true,
    "maxAttempts": 3,
    "delaySeconds": 60
  },
  "schedule": null,
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

Failed first-attempt trigger executions are scheduled through the trigger's configured retry policy. Trigger log DTOs include:

- `retryOfLogId`: the failed source log for manual or automatic retry logs, derived from retry metadata.
- `retryState`: `pending`, `completed`, `exhausted`, or `disabled` when automatic retry state applies.
- `autoRetryAttemptCount`
- `autoRetryMaxAttempts`
- `autoRetryNextAttemptAt`

The hosted retry worker replays due failed logs through the trigger's current action list, matching manual retry semantics. Disabled triggers or triggers with automatic retries disabled are not retried automatically; their pending failed logs surface `disabled`. Retries stop once `autoRetryAttemptCount` reaches `autoRetryMaxAttempts`.

Scheduled trigger logs use `entityType = "Schedule"` and include `schedule` metadata in `input` and `result` JSON. The metadata records due time, lock time, completion time, final status, and skip reason when a persisted due schedule cannot be processed. Action failures still write `failed` logs and use the normal trigger retry policy.

### Retry failed trigger log

`POST /api/triggers/{triggerId}/logs/{logId}/retry`

Requires form `manage` or `forms.manage_all` access for the trigger's form. The source log must belong to the trigger and have `failed` status. The backend replays the saved trigger event input through the trigger's current action list and creates a new trigger execution log. The retry response is `201 Created` with the new log.

Manual and automatic retry logs expose `retryOfLogId` and include retry metadata in the JSON payloads:

```json
{
  "id": "...",
  "triggerId": "...",
  "status": "success",
  "retryOfLogId": "...",
  "input": {
    "eventName": "record.created",
    "retry": { "sourceLogId": "..." }
  },
  "result": {
    "retry": { "sourceLogId": "..." },
    "actions": []
  }
}
```

Retry requests for missing logs, logs from another trigger, disabled triggers, or non-failed logs return an error. If actions fail during the retry, the API still returns the new failed retry log and records the error in that log.

Record submission dispatches `record.created`. Record edits dispatch `record.updated` and dispatch `field.changed` when values changed. Record status changes dispatch `status.changed`. Record assignment changes dispatch `record.assigned`. Trigger execution failures are logged and do not roll back the original record mutation.

## Workflows V5

Workflow APIs require authentication. V5 task 001 management endpoints require form `manage` access for the target form, which is also granted by `forms.manage_all`. V5 task 002 adds the `/workflows` management UI over these APIs. V5 task 003 adds record workflow state, start, and direct transition APIs over published workflow versions. V5 task 004 adds approval-gated transition requests, current-user approval inbox APIs, and in-app notifications for assigned approvers. V5 task 005 executes supported transition actions after direct or approval-completed transitions. V5 task 006 starts workflows from typed trigger actions. V5 task 007 adds a frontend-only visual builder that still reads and writes the same typed workflow definition config through the existing create/update/publish endpoints.

Workflow definitions are scoped to one form. The mutable definition stores a draft config and points to the current published immutable version when published. Future workflow history can reference the exact `workflowDefinitionVersionId` used for a record.

Workflow config supports typed:

- states: `{ "key": "manager_review", "name": "Manager Review", "isFinal": false }`
- transitions: `{ "key": "submit", "name": "Submit", "fromStateKey": "draft", "toStateKey": "manager_review", "approvalStepKey": "manager_approval" }`
- approval steps: `{ "key": "manager_approval", "name": "Manager approval", "mode": "any", "assigneeRules": [...] }`
- assignee rules: `user`, `group`, `department_manager`, and `record_owner`
- optional transition actions: typed actions such as `write_audit_entry`, `send_email`, `send_notification`, `assign_record`, `update_field`, and `create_record`; `change_status`, `call_webhook`, PDF, external integration, retry-queue, and custom-code actions are rejected or left out of this slice.

Validation rejects duplicate state/transition/approval keys, missing or final initial states, missing final states, invalid transition endpoints, transitions from final states, invalid approval references, unsupported approval modes, missing assignee rules, assignee targets that are not active users, groups, or departments, and unsupported workflow action types.

### List workflows

`GET /api/forms/{formId}/workflows`

Requires form `manage` or `forms.manage_all` access. Response: `200 OK` with `{ "items": [...] }`.

### Create workflow

`POST /api/forms/{formId}/workflows`

Requires form `manage` or `forms.manage_all` access. Creating a workflow writes a `workflow_created` audit entry.

Request:

```json
{
  "name": "Employee approval",
  "description": "Route employee records through manager review.",
  "config": {
    "schemaVersion": 1,
    "initialStateKey": "draft",
    "states": [
      { "key": "draft", "name": "Draft" },
      { "key": "manager_review", "name": "Manager Review" },
      { "key": "approved", "name": "Approved", "isFinal": true }
    ],
    "transitions": [
      { "key": "submit", "name": "Submit", "fromStateKey": "draft", "toStateKey": "manager_review", "approvalStepKey": "manager_approval" },
      { "key": "approve", "name": "Approve", "fromStateKey": "manager_review", "toStateKey": "approved" }
    ],
    "approvalSteps": [
      {
        "key": "manager_approval",
        "name": "Manager approval",
        "mode": "any",
        "assigneeRules": [
          { "type": "department_manager", "departmentId": "..." }
        ]
      }
    ]
  },
  "isEnabled": true
}
```

Response: `201 Created` with the saved workflow detail.

### Get workflow

`GET /api/workflows/{workflowId}`

Requires form `manage` or `forms.manage_all` access for the workflow's form. Response: `200 OK` with the saved workflow detail, draft config, status, enabled state, current published version metadata, unpublished-change flag, and concurrency stamp.

### Update workflow

`PUT /api/workflows/{workflowId}`

Requires form `manage` or `forms.manage_all` access for the workflow's form. The request shape matches create and also requires `concurrencyStamp`. Updating the draft config writes `workflow_updated`; enabled-state changes through update also write `workflow_enabled` or `workflow_disabled`.

### Publish workflow

`POST /api/workflows/{workflowId}/publish`

Requires form `manage` or `forms.manage_all` access and `{ "concurrencyStamp": "..." }`. Publishing validates the current draft config, creates a new immutable `workflow_definition_versions` row, updates `currentVersionId`, clears `hasUnpublishedChanges`, and writes `workflow_published`.

### Enable or disable workflow

`POST /api/workflows/{workflowId}/enable`

`POST /api/workflows/{workflowId}/disable`

Both require form `manage` or `forms.manage_all` access and `{ "concurrencyStamp": "..." }`. These endpoints toggle workflow availability without deleting definitions, versions, or history and write `workflow_enabled` or `workflow_disabled`.

### Get record workflow state

`GET /api/records/{recordId}/workflow`

Requires record `view` access for the record's form and scoped record rules. Response includes the active workflow/version/state when present, available start options when the user can change the record status and no workflow is active, available transitions when the user can change status and the active workflow state has direct or approval-gated transitions, and recent workflow history. Approval-gated transitions are marked with `requiresApproval: true`.

### Start record workflow

`POST /api/records/{recordId}/workflow/start`

Requires record `change_status`, form `manage`, or `forms.manage_all` access for the record's form and scoped record rules.

Request:

```json
{
  "workflowDefinitionId": "11111111-1111-1111-1111-111111111111",
  "concurrencyStamp": "record-stamp"
}
```

The backend rejects disabled, unpublished, wrong-form, or already-active workflows. On success it stores `workflowDefinitionId`, `workflowDefinitionVersionId`, and `workflowStateKey` on the record, updates `records.status` to the initial state key, writes `workflow_started` history plus a `record_workflow_started` audit entry, and dispatches a status-changed trigger event when the record status changed.

### Execute record workflow transition

`POST /api/records/{recordId}/workflow/transitions/{transitionKey}`

Requires record `change_status`, form `manage`, or `forms.manage_all` access for the record's form and scoped record rules.

Request:

```json
{
  "concurrencyStamp": "record-stamp"
}
```

The backend uses the workflow definition version already stored on the record. It rejects unavailable transitions and transitions from another state. Direct transitions update `workflowStateKey`, update `records.status` to the target state key, write `workflow_transitioned` history plus a `record_workflow_transitioned` audit entry, execute configured transition actions from the stored workflow version, and dispatch a status-changed trigger event when the record status changed.

Supported transition actions run in configured order. Action attempts are represented as `workflow_action_succeeded` or `workflow_action_failed` rows in `workflow_history`; each row stores action id, action type, status, error message, started time, completed time, and result metadata in `metadata_json`. `assign_record`, `update_field`, `create_record`, and audit-entry action failures roll back the transition transaction and persist rollback failure history afterward. `send_email` and `send_notification` failures are logged in workflow history without hiding the completed transition outcome. Workflow action record mutations do not recursively dispatch trigger events; the normal status-changed trigger dispatch still happens after the workflow transition commits.

Approval-gated transitions do not immediately move the record. They create one pending `workflow_approval_tasks` row per resolved active approver, write `workflow_approval_requested` history plus `record_workflow_approval_requested` audit, and create in-app notifications for approvers who have in-app notifications enabled. Duplicate pending approval groups for the same record, workflow version, transition, and from-state are rejected.

### List current-user workflow approvals

`GET /api/workflow-approvals`

Requires authentication. The response is scoped to the authenticated persisted user and returns pending and recent approval tasks assigned to that user.

Response:

```json
{
  "items": [
    {
      "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      "approvalGroupId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      "workflowDefinitionId": "11111111-1111-1111-1111-111111111111",
      "workflowDefinitionVersionId": "22222222-2222-2222-2222-222222222222",
      "formId": "33333333-3333-3333-3333-333333333333",
      "recordId": "44444444-4444-4444-4444-444444444444",
      "approvalStepKey": "manager_approval",
      "approvalStepName": "Manager approval",
      "mode": "any",
      "transitionKey": "submit",
      "transitionName": "Submit",
      "fromStateKey": "draft",
      "toStateKey": "manager_review",
      "status": "pending",
      "assignedToUserId": "55555555-5555-5555-5555-555555555555",
      "requestedById": "66666666-6666-6666-6666-666666666666",
      "respondedById": null,
      "respondedAt": null,
      "comment": null,
      "createdAt": "2026-06-04T14:00:00Z"
    }
  ]
}
```

### Approve workflow approval

`POST /api/workflow-approvals/{approvalTaskId}/approve`

Requires authentication and the task must be assigned to the current user. Request body:

```json
{
  "comment": "Looks good."
}
```

For `any` mode, the first approval executes the transition and cancels pending sibling tasks. For `all` mode, the record moves only after all assigned approvers have approved. Completed transitions write approval history/audit, `workflow_transitioned` history, `record_workflow_transitioned` audit, requester notification when applicable, execute transition actions through the same backend path as direct transitions, and dispatch the existing status-changed trigger event when the record status changed.

### Reject workflow approval

`POST /api/workflow-approvals/{approvalTaskId}/reject`

Requires authentication and the task must be assigned to the current user. Rejection writes approval rejection history/audit, cancels pending sibling tasks, notifies the requester when applicable, and leaves the record in its current workflow state.

### V5 visual workflow builder API behavior

The V5 visual workflow builder does not introduce a new backend endpoint, database field, or layout metadata contract. Visual state, transition, approval, and action edits are serialized back into the same workflow definition config accepted by the existing create/update/publish endpoints.

## API Rules

- All APIs must validate input.
- All record APIs must check permissions.
- Hidden fields must not be returned to unauthorized users.
- Mutating APIs should write audit logs.
- Record create/update/status/assignment APIs dispatch V4 trigger events after the primary record transaction succeeds.
