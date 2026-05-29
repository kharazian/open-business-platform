# Roadmap

## Current State: V1 Finalized, V2 Started

- ASP.NET Core minimal API host exists.
- React frontend shell exists.
- Shared UI/layout components exist.
- Frontend app modules and permission-aware route/navigation registry exist.
- Backend minimal API module discovery exists.
- Authenticated dashboard summary endpoint exists.
- Shared V1 form schema contracts and validators exist in frontend and backend code.
- EF Core/Npgsql database foundation exists for users, roles, role permissions, form permissions, departments, forms, form versions, records, and audit logs.
- Cookie auth exists with bootstrap admin fallback and local PostgreSQL user login.
- Users & Access workspace exists for local users, roles, menu permissions, and per-form role access.
- Persistent Forms list/create API and frontend page exist.
- Backend-owned form draft metadata editing, field editing, responsive layout, preview, immutable publishing, and submit-safe published form rendering exist.
- Record submission, list/detail, edit, soft-delete, and browser print flows exist with backend permission checks and audit logs.
- Saved V2 list report definitions exist with column, filter, sort, backend validation, and report management permission checks.
- Development startup seed data exists for demo users, roles, departments, a published sample form, permissions, and records.
- Real app appearance settings exist and are saved in browser localStorage.
- `/theme` includes sample workspace, foundation, authentication, layout, and component demo pages.
- Lightweight frontend logic/API tests exist for module registry, form schema/records, forms API/list/builder/submission helpers, auth, users, records, and shared UI helpers.
- `/theme` playground exists for sample-data design review.
- PostgreSQL and Redis run through Docker Compose.
- V1 finalization checks passed with frontend tests/build, backend harness/build, and compose API smoke checks for auth, forms, records, and permission denials.

Next: continue V2 with form data readiness, report viewer/run behavior, real dashboard summaries, chart/dashboard builder lite, then CSV export and cleaner print layouts.

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

Goal: automate actions after data changes.

Features:

- Trigger list
- Trigger builder
- Event-based triggers
- Conditions
- Actions
- Email/in-app notifications
- Update fields
- Change status
- Assign users
- Webhook call
- Trigger logs

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

Goal: connect to external systems.

Features:

- Webhooks
- API keys
- Scheduled triggers
- Import records
- External exports
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
