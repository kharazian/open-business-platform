# Creator-Style App Support Roadmap

This document captures the capabilities implied by the Order Bridge sample Zoho Creator application. The exported `.ds` file was a reference artifact only and should not be stored in this repository because Creator exports may contain connection names, credentials, tokens, customer data, or other sensitive operational details.

The goal is not to copy Zoho Creator directly. The goal is for Open Business Platform to support the same class of business apps with clear module boundaries, backend-enforced permissions, versioned forms, auditable automation, and safe integration handling.

## Target App Class

The platform should eventually support operational apps that combine:

- Custom business forms and record tables.
- Related records, lookup fields, and parent-child record structures.
- List/detail reports with user actions.
- Scheduled jobs and retryable processing queues (bounded CSV import/export slice implemented in V10 Task 008; partner-specific formats and remote connectors remain later work).
- External integrations such as SFTP, file storage, webhooks, and vendor APIs.
- File/document generation and transfer.
- In-app and email notifications.
- Operational logs, integration logs, and audit history.
- Seed/import tooling for initial setup and migrations from existing systems.

## Capabilities To Add

### 1. Richer Form Field Types

Current form fields cover the core V1/V2 set. Creator-style apps need additional field support:

- Date-time fields with timezone-aware display and storage.
- Time-only fields.
- URL fields.
- Decimal/currency fields with precision rules.
- Autonumber fields.
- Address fields with structured subfields such as line 1, line 2, city, state/province, postal code, country, latitude, and longitude.
- File upload fields with metadata, access rules, scanning hooks, and storage abstraction.
- Subform/grid fields for child rows embedded in a parent record.
- Long text fields for operational payloads, summaries, and errors.

Implementation direction:

- Keep schema definitions separate from React components.
- Add field contracts in frontend and backend together.
- Store flexible field values in JSONB while promoting heavily queried fields only when needed.
- Preserve the submitted form version on every record.

### 2. Lookup and Relationship Model

Creator-style apps rely heavily on relationships such as order-to-items, file-history-to-order, and partner/item master lookups.

Needed support:

- Record lookup fields with display fields, search fields, sort options, and permission-aware query results.
- Nested display values in reports, such as `Parent_Order.Order_Number`.
- One-to-many related record sections on record detail pages.
- Optional parent-child record creation flows.
- Referential integrity rules for deletes, soft deletes, and archived records.
- Backend filtering so hidden or unauthorized lookup data is not exposed.

Implementation direction:

- Extend the existing `recordLookup` concept rather than introducing a separate relationship builder.
- Keep relationship rendering in records/reports modules, not inside the form builder.

### 3. Report Builder and Viewer Upgrades

Current list reports cover saved columns, filters, sorting, execution, CSV export, and print. Creator-style apps need a fuller operational report experience.

Needed support:

- Separate quick-view and detail-view report layouts.
- Report-level action menus for add, edit, duplicate, delete, print, import, export, and view record.
- Permission-aware action availability per report and per row.
- Nested lookup columns.
- Wider filter operators for dates, numbers, booleans, choices, lookups, and empty/not-empty checks.
- Saved report variants for operational queues such as failed files, pending orders, ready-to-send records, and retry queues.
- Better report import/export configuration while preserving backend authorization.

Implementation direction:

- Keep report definitions separate from form definitions.
- Keep report actions as typed action definitions, not arbitrary UI scripts.

### 4. Record Detail and Related Data Workspace

Operational apps need record detail pages that act as workspaces, not only field viewers.

Needed support:

- Related records panels.
- Record timeline with audit, workflow, trigger, and integration events implemented on record detail pages; notification timeline events can be added when notification management expands.
- Safe record actions such as reprocess, retry, hold, release, cancel, send, export, and generate document.
- Field grouping and section visibility in detail views.
- Permission-aware edit/read-only/hidden states.

Implementation direction:

- Build these on top of records, reports, permissions, workflows, triggers, and audit modules.
- Do not make the form builder own operational record workflows.

### 5. Integration Connectors

Creator-style apps often combine records with external systems.

Needed support:

- SFTP connector configuration with host, port, paths, configured secret names, and active state is now persisted as secret-safe connector metadata.
- File storage connector abstraction for systems such as WorkDrive, S3-compatible storage, or local development storage now has a stored connector type and metadata foundation.
- Vendor API connector configuration for systems such as Shopify or ERP/EDI providers now has a stored connector type and metadata foundation.
- Webhook listener and outbound webhook support.
- Secret storage with encryption or provider-backed secret management remains a later execution-layer improvement; current connector configs discard raw secret values and keep only configured secret names.
- Credential masking in UI and API responses is implemented for connector config metadata and existing key/webhook surfaces.
- Connector test actions with audit logs.

Implementation direction:

- Store secrets outside normal form records.
- Never expose raw secret values after create/rotate.
- Reuse integration logs for every inbound/outbound attempt.

### 6. Import, Export, and File Processing Jobs

The platform needs background-style operations for order/file processing apps.

Needed support:

- File import jobs for CSV, JSON, EDI, and other structured payloads.
- Mapping definitions from external payload fields to form fields.
- File history records with status, direction, source system, document type, remote path, work file ID, and related record links.
- Export jobs that generate protected CSV/JSON artifacts are implemented; EDI-like text, PDF, and partner-specific payloads remain later extensions.
- Retry state including retry count, max attempts, retry interval, last retry time, and next retry time.
- Safe file deletion/archive operations after successful processing when configured.

Implementation direction:

- Treat file processing as integration jobs, not as form scripts.
- Keep generated files and artifacts permission-filtered and auditable; protected export artifact downloads are implemented without public links.

### 7. Scheduled Automation and Queues

Creator-style apps often use scheduler pages or scheduled functions to pull files, process queues, and send outbound documents.

Needed support:

- Scheduler run records with start time, end time, duration, status, and error message.
- Named job definitions for pull, process inbound, process outbound, fulfillment, invoicing, cleanup, and retry.
- Bounded queue processing to avoid unbounded automation runs.
- Job locks so the same scheduled job does not run concurrently.
- Per-job logs and summary metrics.
- Manual run/retry controls with permissions and audit logs.

Implementation direction:

- Extend the trigger/scheduled automation foundation with typed job definitions.
- Keep arbitrary custom code out of user-authored automations.

### 8. Safe Action Engine Expansion

The sample implies many actions that should exist as typed, approved backend actions.

Needed support:

- Create/update related records.
- Update statuses and operational fields.
- Send email notifications.
- Send in-app notifications.
- Generate text/JSON/CSV/EDI-like files from templates.
- Generate PDFs from print templates.
- Upload/download/delete files through configured connectors.
- Call external APIs through configured connectors.
- Start workflows and scheduled workflow starts.
- Link action results to integration logs and audit logs.

Implementation direction:

- Every action should have a typed contract, validation, permissions, execution logs, and retry behavior where appropriate.
- Do not support arbitrary user code until there is a sandboxed, audited execution model.

### 9. Operational Logging and Audit

Creator-style business apps need both audit logs and operational logs.

Needed support:

- Function/job/action logs with priority, message, parameters, error details, line or step metadata, user, and timestamp.
- Integration logs for inbound/outbound attempts.
- Trigger and workflow execution logs.
- Retry logs with source failure links.
- Error notification state to avoid duplicate alerts.
- Searchable operational log reports.

Implementation direction:

- Keep audit logs for security/business history.
- Keep operational logs for processing diagnostics.
- Sanitize payloads before storing logs.

### 10. Notifications and Error Handling

Needed support:

- Configurable error notification recipients.
- Email and in-app notifications for failed jobs, retries exhausted, blocked records, and manual-review queues.
- Notification preferences by user or role.
- Safe suppression/deduplication for repeated failures.
- Links from notifications back to records, logs, jobs, and reports.

Implementation direction:

- Reuse the notifications module.
- Keep notification dispatch auditable and permission-aware.

### 11. Configuration and Settings

Creator-style apps often use settings forms for operational configuration.

Needed support:

- Workspace-level settings separate from ordinary user-submitted records.
- Partner/customer configuration records.
- Item/master-data records.
- Connection configuration records without raw secret exposure.
- Versioned settings changes where operational behavior changes materially.
- Permission checks for settings changes.

Implementation direction:

- Use normal forms for non-sensitive master data where practical.
- Use dedicated integration/settings tables for secrets and system-owned runtime configuration.

### 12. Importer From Creator-Style Exports

Long term, the platform should help migrate existing Creator apps.

Needed support:

- A parser or guided import tool for Creator-style exports.
- Mapping from Creator field types to platform field types.
- Mapping from Creator reports to saved list reports.
- Detection of unsupported fields, functions, pages, workflows, connections, and permissions.
- A migration report that lists what imported, what needs manual setup, and what is unsafe.
- Secret redaction and credential-blocking during import.

Implementation direction:

- Start with an analysis-only importer before creating records/forms.
- Never import credentials from exported source files.
- Require explicit user confirmation before creating forms/reports from imported definitions.

## Suggested Roadmap Placement

These capabilities should be added incrementally after the current V8 integration foundation and alongside the V9 enterprise sequence where appropriate.

Recommended order:

1. Richer form field types.
2. Lookup/relationship upgrades.
3. Report quick-view/detail-view upgrades.
4. Record detail workspace and related records.
5. Secret-safe connector configuration.
6. File processing jobs and artifacts.
7. Scheduled job definitions and queue controls.
8. Safe action engine expansion.
9. Operational logging and notification improvements.
10. Creator-style export analyzer/import assistant.

## Non-Goals

- Do not store exported Creator `.ds` files with secrets in the repository.
- Do not clone Zoho-specific UI or scripting behavior directly.
- Do not add arbitrary custom code execution as a shortcut.
- Do not weaken backend permission enforcement to match client-side Creator behavior.
- Do not merge forms, reports, triggers, workflows, integrations, and printing into one giant builder.
