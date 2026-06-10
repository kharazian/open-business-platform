# V8 Task Index

V8 adds integration and API capabilities on top of the V1-V7 product foundation.

The sequence is intentionally conservative: authenticate integrations first, make every integration action observable, then add inbound and outbound data paths one slice at a time.

## Recommended Execution Order

1. `001-api-keys-and-integration-auth.md` - complete; hashed API keys, scoped integration identities, and backend auth plumbing.
2. `002-integration-logs-and-retry-foundation.md` - complete; shared integration log records, statuses, sanitized metadata, and retry-safe metadata.
3. `003-public-record-api-foundation.md` - complete; authenticated public/internal record API endpoints with existing permission and field rules.
4. `004-incoming-webhook-listeners.md` - complete; named webhook listeners that can create or update records through safe typed mappings.
5. `005-record-import-jobs.md` - complete; CSV/import job foundation for permitted form records with validation and audit logs.
6. `006-external-export-jobs.md` - complete; outbound export jobs for permitted report/record data with integration logs.
7. `007-scheduled-automation-expansion.md` - complete; explicit daily/weekly/monthly schedule contracts, tested due-time calculation, safe action validation, and schedule run log metadata.
8. `008-scheduled-workflow-starts.md` - complete; safe scheduled workflow starts with explicit record selection rules.
9. `009-integration-operations-ui.md` - admin UI for API keys, integration logs, retries, and webhook/import/export status.

## Scope Rules

- Keep API keys, webhooks, imports, exports, logs, retries, and scheduled automation as integration module work.
- Reuse existing backend permission services, record scopes, report permissions, and field-hidden rules.
- Do not expose hidden field values through public APIs, imports, exports, logs, or webhook responses.
- Keep custom code execution out of V8.
- Do not add arbitrary SQL, cross-form joins, or broad external database sync in the first V8 sequence.
- Keep workspace ownership and tenant-level policies for a later enterprise module.
- Add tests where practical and document every new endpoint, table, or security-sensitive behavior.
