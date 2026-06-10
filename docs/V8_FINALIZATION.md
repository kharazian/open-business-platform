# V8 Finalization

V8 is complete for the current task list.

## Scope Completed

V8 connected the platform to external systems while preserving the existing permission, audit, schema, and module boundaries.

Completed V8 work:

1. Hashed integration API keys and API-key authentication plumbing.
2. Integration logs with sanitized metadata and explicit retry request metadata.
3. Versioned API-key-authenticated record list/read/create endpoints.
4. Incoming webhook listeners with typed mappings and hashed listener secrets.
5. CSV record import jobs with row-level validation results.
6. External export jobs for permission-filtered form records and list reports.
7. Daily/weekly/monthly scheduled automation contracts.
8. Scheduled workflow starts over safe same-form record selections.
9. Permission-aware `/integrations` operations UI for API keys, logs, and retry requests.

## Security And Architecture Checks

- API keys and listener secrets are stored hashed; raw values are returned only on create/rotate responses.
- Integration entry points reuse backend permissions, record scopes, field visibility rules, and validation.
- Hidden field values are not exposed through public record APIs, exports, logs, or webhook responses.
- Integration metadata is sanitized before persistence.
- Sensitive integration actions write audit logs.
- Retry is explicit and observable through integration log metadata; V8 does not add arbitrary background replay.
- Integrations remain separate from reports, dashboards, triggers, workflows, printing, audit, and notifications.
- V8 does not add custom code execution, arbitrary SQL, cross-form joins, anonymous public links, workspace ownership, or tenant-level policy.

## Implemented User Surfaces

- `/integrations` for administrators with `integrations.manage`.
- API key create/revoke/rotate controls.
- Sanitized integration log list, filters, detail review, and retry request action.

## Implemented API Areas

- `/api/integrations/api-keys`
- `/api/integrations/logs`
- `/api/integration/v1/forms/{formId}/records`
- `/api/integration/v1/records/{recordId}`
- `/api/integrations/webhooks/listeners`
- `/api/integrations/webhooks/{listenerKey}`
- `/api/integrations/import-jobs`
- `/api/integrations/export-jobs`

See `docs/API_SPEC.md` for endpoint details.

## Implemented Data Areas

- `integration_api_keys`
- `integration_logs`
- `incoming_webhook_listeners`
- `record_import_jobs`
- `record_import_job_rows`
- `external_export_jobs`
- trigger schedule metadata on existing trigger definitions/logs

See `docs/DATA_MODEL.md` for table details.

## Known Limits Deferred Past V8

- No marketplace connector catalog.
- No arbitrary custom integration code.
- No broad external database sync.
- No anonymous/public form or report links.
- No tenant/workspace ownership model.
- No advanced SSO or enterprise identity policy.
- No automated retry worker for integrations beyond explicit retry request metadata.

## Verification For Final V8 State

Run before closing or branching from V8:

```bash
dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj
dotnet build src/api/OpenBusinessPlatform.Api.csproj
cd src/app
npm test
npm run build
```

Run before committing documentation or code changes:

```bash
git diff --check
```

## Next

V9 can be postponed. When ready, use `docs/V9_START_HERE.md` and `tasks/v9/README.md`.
