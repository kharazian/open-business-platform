# Roadmap

## Current State: V1-V10 Complete

- ASP.NET Core minimal API host exists.
- React frontend shell exists.
- Shared UI/layout components exist.
- Frontend app modules and permission-aware route/navigation registry exist.
- Backend minimal API module discovery exists.
- Authenticated, permission-checked, database-backed dashboard summary endpoint exists.
- Shared V1 form schema contracts and validators exist in frontend and backend code.
- EF Core/Npgsql database foundation exists for users, roles, role permissions, form permissions, departments, forms, form versions, records, and audit logs.
- Cookie auth exists with bootstrap admin fallback and local PostgreSQL user login.
- Users & Access workspace exists for local users, roles, menu permissions, and per-form role access.
- Users & Access workspace includes users, roles, departments, groups, scoped form access, report access, and field rules.
- Persistent Forms list/create API and frontend page exist.
- Backend-owned form draft metadata editing, field editing, responsive layout, preview, immutable publishing, and submit-safe published form rendering exist.
- Record submission, list/detail, edit, soft-delete, and browser print flows exist with backend permission checks and audit logs.
- Saved V2 list report definitions exist with column selection, column ordering, custom labels, filters, sort, backend validation, runnable viewing, CSV export, print, and report management permission checks.
- Chart widget previews exist for number cards, bar charts, date trends, choice/status breakdowns, and table widgets over permitted form/report data.
- Workspace-owned saved dashboard definitions exist with PostgreSQL-backed widget/config layout, backend validation, publishing, stable slugs, directory/viewer routes, and permission-filtered navigation.
- Development startup seed data exists for demo users, roles, departments, a published sample form, permissions, and records.
- Real app appearance settings exist and are saved in browser localStorage.
- `/theme` includes sample workspace, foundation, authentication, layout, and component demo pages.
- Lightweight frontend logic/API tests exist for module registry, form schema/records, forms API/list/builder/submission helpers, auth, users, records, reports, dashboards, printing, and shared UI helpers.
- `/theme` playground exists for sample-data design review.
- PostgreSQL and Redis run through Docker Compose.
- V1-V7 verification has passed with frontend tests/build, backend harness/build, and local API smoke checks where applicable.
- Backend workflow definition persistence, publishing/versioning, validation, management APIs, permission checks, audit logs, and history foundation exist.
- `/workflows` frontend management UI exists for form-scoped workflow definition list/create/edit/publish/enable/disable operations over JSON-backed configs.
- Record workflow start/direct transition execution and current-user approval inbox execution exist.
- Workflow transition action execution exists for the safe V5 action subset.
- Trigger actions can start eligible published workflows on current records without recursive automation loops.
- `/workflows` includes a workflow-only XYFlow visual builder over the existing typed draft config, with JSON fallback and no persisted graph layout metadata.
- `/printing` includes persisted record/report print template management, immutable published versions, safe small logo uploads, browser print/save-as-PDF output, dependency-light server-side PDF downloads, and trigger email record PDF attachments.
- Backend integration API key management exists with hashed key storage, conservative typed scopes, `integrations.manage` management endpoints, create/revoke/rotate audit logs, and API-key authentication plumbing.
- Backend integration log persistence exists with typed directions/types/statuses, sanitized metadata, retry metadata, read endpoints, and auditable explicit retry requests.
- Versioned API-key-authenticated record list/read/create endpoints exist under `/api/integration/v1`, reusing linked-user form permissions, record scopes, validation, hidden-field filtering, audit logs, and integration logs.
- Named incoming webhook listeners exist with persisted typed mappings, hashed listener secrets, API-key or listener-secret authentication, create/safe-lookup upsert record execution, backend permission checks, and inbound integration logs.
- CSV record import jobs exist with explicit field mappings, persisted status, row-level success/failure results, existing record validation/permissions, audit logs, and inbound import integration logs.
- External export jobs exist for permitted form records and list reports with CSV/JSON artifacts, hidden-field filtering, persisted metadata/content, audit logs, and outbound export integration logs.
- Scheduled trigger contracts include explicit daily/weekly/monthly interval and day metadata, tested due-time calculation, safe action validation, and due/locked/skipped/success/failure trigger log metadata.
- Scheduled workflow starts exist for explicit same-form workflow targets and bounded record selection rules, with workflow history/audit entries and trigger log record results.
- `/integrations` exists as a permission-aware operations workspace for API keys, webhook listeners, imports/exports, integration logs, bounded processing jobs/diagnostics/failure policies, and analysis-only Creator export review.

V10 operational app modeling is finalized through Task 010 in `docs/V10_FINALIZATION.md`. Any later product scope requires a separately reviewed plan.

The Zoho Creator-style Order Bridge sample should be treated as a product expectation reference, not a source artifact. The platform capabilities needed to support that class of operational app are captured in `docs/CREATOR_APP_SUPPORT_ROADMAP.md`.

## Product Engine Path

The long-term product should be built as cooperating engines:

- **Form engine:** create forms, edit drafts, publish immutable versions, open forms, and show form details.
- **Record engine:** create, open, edit, show details, soft-delete, audit, and print individual records.
- **Report engine:** show each form's records in table reports with saved columns, filters, search, sorting, pagination, permissions, export, and print.
- **Print engine:** support clean single-record print and report table print first, then PDF/template output later.
- **Validation/rule engine:** enforce field validation first, then conditional record rules.
- **Trigger engine:** start automation from record events, status/field changes, schedules, and webhooks.
- **Workflow engine:** define multi-step status transitions, approvals, assignments, and workflow history.
- **Action engine:** provide safe workflow/trigger actions such as create/update record, send email, call API/webhook, generate document later, and start another workflow.

The reachable sequence is:

1. Finish the form and record data spine.
2. Build runnable reports and cleaner print output on top of that data.
3. Add validation/rule definitions that can be reused by records, reports, triggers, and workflows.
4. Add event triggers and a small action engine.
5. Add workflow definitions and a workflow runner.
6. Add scheduled triggers, webhook/integration triggers, and richer action connectors.

## V1: Foundation - Forms and Records

Goal: create a working product foundation.

Status: complete and verified.

Features:

- Existing auth integration or simple user model
- Basic roles
- Form list
- Create form
- Field builder
- Responsive layout builder
- Form preview
- Publish form version
- Submit form
- Store records
- Record list
- Record detail
- Edit/delete record
- Basic browser print
- Basic permission checks
- Basic audit logs
- Seed data

## V2: Form Data, Reports, Dashboards, Charts, and Better Printing

Goal: turn submitted form data into runnable reports, dashboard summaries, simple charts, exports, and cleaner printed views.

Features:

- Form data readiness for reporting
- List report builder
- Column selection
- Column ordering
- Filters
- Sorting
- Search
- Saved reports
- Runnable report viewer
- Real dashboard summary API
- Chart builder lite
- Dashboard builder lite
- CSV export
- Cleaner print layouts
- Basic report permissions

## V3: Advanced Permissions

Goal: add organization-aware access control.

Status: complete for the current repository.

Features:

- Users, roles, groups, departments
- Department manager model
- Form-level permissions
- Report-level permissions
- Record-level permissions
- Action-level permissions
- Own records only
- Department records only
- Group records only
- Assigned records only
- Basic field-level visibility/read-only

## V4: Trigger Engine

Goal: automate safe, auditable actions after data changes.

Implemented foundation:

- Event-based triggers
- On record created
- On record updated
- On field changed
- On status changed
- On record assigned
- Conditions
- Action engine foundation
- Audit entry action
- Send email action
- Change status
- Assign users
- Update fields
- Trigger logs
- Trigger list UI
- Trigger builder UI
- Trigger logs UI
- Manual retry for failed trigger logs
- In-app notification action
- Current-user notification inbox
- Notification unread count and read state APIs
- Notification unread badges and preferences
- Create related records
- Automatic retry queue for failed trigger logs
- Webhook call action
- User-authored retry policy controls
- Scheduled trigger runner for safe email/webhook actions

Future V4 work: complete for the current roadmap scope.

## V5: Workflow and Approval

Goal: support multi-step processes.

Implemented foundation:

- Status states
- Transitions
- Approval steps
- Single and multiple approvers
- Department manager approval
- Workflow history
- Backend workflow definition management APIs
- Draft/edit/publish workflow definition versioning
- Workflow validation and mutation audit logs
- Workflow management UI
- Record workflow transition execution
- Approval inbox and in-app approval notifications
- Workflow transition action execution
- Trigger-to-workflow starts
- Optional XYFlow visual workflow builder over the existing typed config

## V6: Print Templates and PDF

Goal: professional printable documents.

Features:

- Print template foundation implemented: persisted record/report templates, JSONB config, backend validation/permissions/audit, `/printing` management UI, and selected record/report browser print/save-as-PDF output.
- Header/footer/logo uploads, field/table sections, signature blocks, page setup, repeated report table headers, section page-break controls, and field-based conditional sections are supported in schema version 1 templates.
- Immutable published template versions, dependency-light server-side PDF downloads, and trigger email record PDF attachments are implemented.

## V7: Advanced Dashboards and Analytics

Goal: deepen the V2 dashboard foundation into richer analytics.

Features:

- Advanced summary reports
- Typed dashboard analytics execution implemented in V7 task 001
- Dashboard widget builder upgrade implemented in V7 task 002
- Dashboard viewer refresh implemented in V7 task 003 with per-widget refresh, loading, error, permission, and stale-source states
- Dashboard sharing/default foundation implemented in V7 task 004 with backend-enforced workspace/private visibility and shared default metadata
- Richer charts
- Advanced dashboard builder
- Number cards
- Pending approvals
- Status/department summaries

## V8: Integrations and API

Goal: connect to external systems and expand scheduled automation.

Features:

- Webhooks
- API keys implemented
- Incoming webhook listeners implemented
- Record import jobs implemented
- Scheduled triggers implemented
- Daily/weekly/monthly trigger definitions implemented
- Scheduled workflow starts implemented
- Import records
- External exports implemented
- External API calls from approved action definitions
- Integration logs implemented
- Integration operations UI implemented
- Retry failed integrations

## Creator-Style Operational App Support

Goal: support the class of operational business apps currently built in systems such as Zoho Creator, while preserving this platform's module boundaries and backend authorization model.

Planning doc: `docs/CREATOR_APP_SUPPORT_ROADMAP.md`

Future capability areas:

- Richer form fields such as datetime, time, URL, address, file, autonumber, decimal/currency, and subform/grid fields.
- Stronger lookup and relationship modeling for parent-child records and nested report columns.
- Report quick-view/detail-view layouts with permission-aware row and header actions.
- Record detail workspaces with related records, timelines, and typed operational actions.
- Secret-safe integration connector configuration for SFTP, file storage, vendor APIs, and webhooks.
- File processing jobs, import/export jobs, generated artifacts, and retry state.
- Scheduled job definitions, queue controls, locks, and manual run/retry actions.
- Safe action engine expansion for file generation, connector operations, external API calls, record mutations, notifications, workflow starts, and document generation.
- Operational logs alongside audit, trigger, workflow, and integration logs.
- Creator-style export analysis/import tooling with credential redaction and unsupported-feature reporting.

## V9: Enterprise Platform

Goal: mature platform capabilities.

Features:

- Multi-tenant workspace foundation implemented in V9 task 001 with stable default ownership, migration backfill, active-workspace query filters, and cross-workspace write guards
- Workspace membership and active-user context implemented in V9 task 002 with migration backfill, signed cookie/API-key workspace claims, per-request membership validation, workspace switching, and workspace-scoped user management
- OIDC SSO foundation implemented in V9 task 003 with secret references, protected authorization state, PKCE, validated external identity linking, and active-membership enforcement
- Advanced RBAC/ABAC implemented in V9 task 004 as workspace-scoped, deny-overrides policies layered over existing platform, form, report, and record grants
- Data retention/legal-hold foundation implemented in V9 task 005 with payload-free dry-runs and no destructive executor
- Administrative backup/export foundation implemented in V9 task 006 with checksummed workspace snapshots, protected audited downloads, and validation-only restore plans
- White labeling foundation implemented in V9 task 007 with safe workspace branding, public login projection, permission-gated administration, and real-app chrome integration
- Localization foundation implemented in V9 task 008 with workspace locale/timezone defaults, per-user overrides, and shared formatting/message helpers
- Custom domains implemented in V9 task 009 with normalized global hostname ownership, DNS TXT verification, audited lifecycle controls, and signed-workspace conflict rejection
- Compliance and audit administration implemented in V9 task 010 with operational posture evidence, payload-safe audit search, and bounded audited CSV export

## V10: Operational App Modeling

Goal: support richer operational business applications on top of the V9 enterprise boundary.

Completed sequence:

- Structured address fields implemented in V10 task 001 with bounded configuration/value contracts and stable record/report/print display.
- Backend-generated autonumber fields implemented in V10 task 002 with bounded formatting, immutable record values, and transactional PostgreSQL allocation.
- Protected file attachment storage implemented in V10 task 003 with bounded inspection, private PostgreSQL content, atomic record claims, and audited permission-checked downloads.
- Lookup relationship integrity implemented in V10 task 004 with canonical edges, delete restriction, legacy detection, and archive-safe existing selections.
- Nested report relationship data implemented in V10 task 005 with permission-safe one-hop catalogs, filters, search, sort, CSV, and print.
- Permission-aware related-record workspaces implemented in V10 task 006 with bounded reverse lookup panels, canonical/legacy compatibility, immutable-schema validation, and display-only row projections.
- Typed operational report and row actions implemented in V10 task 007 with saved allowlisted definitions, backend-projected availability, fixed-catalog builder controls, and authoritative destination reauthorization.
- Bounded processing jobs implemented in V10 task 008 with durable definitions/runs, bounded imports/exports, fenced scheduling, and safe export retry chains.
- Processing operational logs and deduplicated failure notifications implemented in V10 task 009 with bounded health/retention, typed policies, paginated inbox access, and safe trusted run links.
- An analysis-only Creator-style export assistant implemented in V10 Task 010 with bounded memory-only input, secret-safe typed findings, and no import/apply path.

V10 is complete through Task 010. Specify and review a separate task before adding an apply/import phase or expanding the analyzer grammar.
