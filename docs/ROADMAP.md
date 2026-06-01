# Roadmap

## Current State: V1 Finalized, V2 Started

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
- Persistent Forms list/create API and frontend page exist.
- Backend-owned form draft metadata editing, field editing, responsive layout, preview, immutable publishing, and submit-safe published form rendering exist.
- Record submission, list/detail, edit, soft-delete, and browser print flows exist with backend permission checks and audit logs.
- Saved V2 list report definitions exist with column, filter, sort, backend validation, and report management permission checks.
- Chart widget previews exist for number cards, bar charts, date trends, choice/status breakdowns, and table widgets over permitted form/report data.
- Saved dashboard definitions exist with PostgreSQL-backed widget config/layout JSON, backend validation, and a `/dashboards` builder/viewer. Workspace ownership remains future work.
- Development startup seed data exists for demo users, roles, departments, a published sample form, permissions, and records.
- Real app appearance settings exist and are saved in browser localStorage.
- `/theme` includes sample workspace, foundation, authentication, layout, and component demo pages.
- Lightweight frontend logic/API tests exist for module registry, form schema/records, forms API/list/builder/submission helpers, auth, users, records, and shared UI helpers.
- `/theme` playground exists for sample-data design review.
- PostgreSQL and Redis run through Docker Compose.
- V1 finalization checks passed with frontend tests/build, backend harness/build, and compose API smoke checks for auth, forms, records, and permission denials.

Next: V2 task list is complete. Continue with V3 advanced permissions or start the next approved product slice.

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

Features:

- Trigger list
- Trigger builder
- Event-based triggers
- On record created
- On record updated
- On field changed
- On status changed
- Conditions
- Action engine foundation
- Email/in-app notifications
- Update fields
- Change status
- Assign users
- Create related records
- Webhook call
- Trigger logs
- Retry failed triggers later
- Scheduled triggers later in the automation/integration path

## V5: Workflow and Approval

Goal: support multi-step processes.

Features:

- Status states
- Transitions
- Approval steps
- Single and multiple approvers
- Department manager approval
- Workflow history
- Optional XYFlow visual workflow builder
- Workflow actions such as sending email, updating records, calling APIs, or creating related records through the shared action engine

## V6: Print Templates and PDF

Goal: professional printable documents.

Features:

- Custom print templates
- PDF generation
- Header/footer/logo
- Conditional sections
- Signature blocks
- Attach PDF to email triggers

## V7: Advanced Dashboards and Analytics

Goal: deepen the V2 dashboard foundation into richer analytics.

Features:

- Advanced summary reports
- Richer charts
- Advanced dashboard builder
- Number cards
- Pending approvals
- Status/department summaries

## V8: Integrations and API

Goal: connect to external systems and expand scheduled automation.

Features:

- Webhooks
- API keys
- Scheduled triggers
- Daily/weekly/monthly trigger definitions
- Scheduled workflow starts
- Import records
- External exports
- External API calls from approved action definitions
- Integration logs
- Retry failed integrations

## V9: Enterprise Platform

Goal: mature platform capabilities.

Features:

- Multi-tenant workspaces
- SSO
- Advanced RBAC/ABAC
- Data retention
- Backup/restore
- White labeling
- Localization
- Custom domains
- Compliance features
