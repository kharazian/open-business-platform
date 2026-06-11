# Master PRD / AI Project Context

## 1. Product Summary

This project is a low-code business form platform built on:

- React frontend
- .NET Core / ASP.NET Core backend
- PostgreSQL database

It is not only a form builder. It is a complete platform for:

- Responsive form building
- Record collection and management
- Report creation
- List and detail printing
- Permissions by user, role, group, department, owner, and custom rule
- Trigger-based automation
- Workflow and approval processes
- Print/PDF generation, dashboards, and integrations

The goal is to start simple and grow step by step into a comprehensive internal business application builder.

## 1.1 Current Repository State

The repository currently contains a finalized V1 foundation, not the full product:

- `src/api`: ASP.NET Core minimal API host targeting .NET 10
- `src/app`: React/React Router/Vite/TypeScript/Tailwind frontend
- `src/app/src/components`: shared UI and layout primitives
- `src/app/src/modules` and `src/app/src/platform`: current frontend module registry for permission-aware routes and navigation
- `src/app/src/features/forms`: shared V1 form schema types, validation, forms API client, forms list page, backend-owned form builder, preview renderer, and submit form page
- `src/app/src/features/records`: record list/detail pages, edit/delete helpers, and browser print helpers
- `src/app/src/features/users`: users/access API client, types, and management workspace for users, roles, groups, departments, scoped form access, report access, and field rules
- `src/app/src/features/reports`: current V2 list report definition, report execution, CSV export, and report print API/types/page
- `src/app/src/features/dashboards`: current V2 dashboard summary API client/types, chart builder preview page, and saved dashboard builder/viewer page
- `src/app/src/features/triggers`: current V4 trigger management API client, builder helpers, form-scoped trigger workspace, logs viewer, failed-log retry UI, automatic retry status display, notification/webhook action editors, retry policy controls, and schedule metadata controls
- `src/app/src/features/workflows`: current V5 workflow management API client, builder helpers, workflow-only visual graph builder, typed definition/record execution/approval contracts, form-scoped workflow management page for definition draft/edit/publish/enable/disable operations, and current-user approval inbox page
- `src/app/src/features/notifications`: current V4 notification inbox API client/types/page for current-user notifications, read state, unread badges, and preferences
- `src/app/src/context/AuthContext.tsx`: cookie-auth session state and effective frontend permissions
- `src/api/Modules/Forms`: shared V1 form schema contracts, backend validation, forms list/create/draft/publish endpoints, submit-safe published form endpoint, and form access options for role permission setup
- `src/api/Modules/Records`: record submission, list/detail, edit, soft-delete, backend value validation, permission checks, and audit logging
- `src/api/Modules/Reports`: current V2 list report definition, execution, and CSV export endpoints, config validation, permission checks, and report audit logging
- `src/api/Infrastructure/Persistence/DemoDataSeeder.cs`: development startup seed data for demo users, roles, departments, a published sample form, permissions, and records
- `src/api/Modules/Identity`: bootstrap-admin fallback, local user login, self-service password recovery email for persistent users, users/roles/groups/departments management endpoints, password hashing, scoped permission service, and field access service
- `src/api/Modules/Notifications`: current email sender abstraction for password recovery plus current-user notification inbox APIs/read-state/preference service
- `src/api/Modules/Triggers`: current V4 backend trigger definitions, validation, detail/list/create/update APIs, event dispatch, starter action execution, trigger logs, manual failed-log retry recovery, automatic retry queue worker, in-app notification trigger action, webhook action, user-authored retry policy handling, and scheduled trigger worker with explicit daily/weekly/monthly schedule contracts
- `src/api/Modules/Workflows`: current V5 backend workflow definitions, typed validation, list/create/read/update/publish/enable/disable APIs, immutable version publishing, record workflow state/start/direct transition execution, approval-gated transition tasks, current-user approval APIs, approval notifications, and workflow history writes
- `src/api/Modules/Dashboard`: current database-backed dashboard summary API, V2 chart widget preview module, and V7 dashboard analytics execution API
- `src/api/Modules/Dashboards`: current V2 saved dashboard definition API with config/layout validation, permission checks, and audit logging
- `src/api/Modules/Integrations`: current V8 API key management, integration log, public record API, incoming webhook listener, record import, and external export module with hashed keys/secrets, conservative scopes, backend permission checks, audit logging, API-key authentication plumbing, sanitized integration log metadata, explicit retry request metadata, versioned API-key-authenticated record list/read/create endpoints, typed webhook payload mapping into records, CSV import jobs with row-level results, and permission-filtered CSV/JSON export jobs
- `src/app/src/features/integrations`: current V8 integrations operations UI for API key lifecycle actions, webhook listener operations, CSV import jobs, export jobs, sanitized integration log review, client-side log filters, and explicit retry requests
- `src/api/Infrastructure/Persistence`: EF Core/Npgsql DbContext and migrations for users, password reset tokens, roles, groups, departments, role permissions, scoped form permissions, report permissions, field permissions, forms, form versions, records, audit logs, current V2 report definitions, saved dashboard definitions, V4 trigger definitions, trigger logs, automatic trigger retry metadata, V4 trigger schedule/retry policy metadata, V5 workflow definitions/versions/history, V6 print templates, notifications, notification preferences, integration API keys, integration logs, incoming webhook listeners, record import jobs, and external export jobs
- `src/app/src/context/AppThemeContext.tsx`: real app appearance settings saved in browser `localStorage`
- `src/app/src/context/ThemeAppearanceContext.tsx`: separate `/theme` playground appearance settings
- `src/app/src/theme`: sample-data theme playground
- `docker-compose.yml`: PostgreSQL and Redis
- `npm test` in `src/app`: lightweight TypeScript logic tests for module registry, form schema/records, forms API/list/builder/submission helpers, auth, users API/types, record edit/print helpers, print template helpers, trigger and workflow API/builder helpers, and shared UI helpers

Treat settings/profile pages as starter UI. Forms, Users & Access, records, record-level permissions, browser print, startup demo data, password recovery email for persistent users, and core audit logs are the finalized V1 baseline.
V2 is complete for the current task list: saved list report definitions, column ordering, custom column labels, runnable report viewing, CSV export, chart previews, saved dashboard layouts, cleaner print layouts, and real database-backed dashboard summaries are implemented.
V3 is complete for the current task list: groups, departments, department managers, scoped record access, report permissions, action permissions, assignment/status record actions, and basic field hidden/read-only rules are implemented.
V4 is complete through task 010: trigger definitions, trigger logs, management APIs, record event dispatch, supported conditions, audit/email/status/assignment starter actions, trigger management UI, `update_field`, manual retry, in-app notifications/inbox/badges/preferences, `create_record`, automatic retry queues, `call_webhook`, user-authored retry policy controls, and scheduled triggers for safe email/webhook actions.
V5 is complete through task 007: workflow definition persistence, management UI, record workflow state, published starts, direct transitions, approval inbox/notifications, transition action execution, trigger-to-workflow starts, and an optional workflow-only XYFlow visual builder over the typed draft config.
V6 print template foundation is complete through task 007: persisted record/report templates, permission-protected APIs, validation, audit logs, `/printing` management UI, selected record/report template rendering, browser print/save-as-PDF generation, page setup, repeated table headers, section page-break controls, conditional sections, immutable published template versions, safe small logo uploads, dependency-light server-side PDF downloads, and trigger email record PDF attachments.
V7 is complete through task 004: a dashboard analytics execution API now supports typed summary, breakdown, trend, and table requests over permission-filtered form or saved list report records without replacing V2 chart previews or saved dashboard definitions; the saved dashboard builder can configure V7 analytics widgets while preserving the existing saved chart config contract; the saved dashboard viewer renders those widgets with per-widget loading, retry, empty, permission, and stale-source states; and conservative dashboard visibility/default settings are backend-enforced through existing dashboard JSONB metadata without adding workspace ownership.
Advanced notification delivery, report/scheduled PDF attachments, custom code, and workspace ownership for dashboards remain later modules. The settings page currently persists real app appearance preferences only; it does not persist workspace settings to the backend. Build product modules through the task files under `tasks/`.

V6 task 003 is complete for field-based conditional print template sections over already-permission-filtered record/report data.
V6 task 004 is complete for immutable published print template versions, publish/history APIs, builder publish controls, and latest-published rendering for selected record/report templates.
V6 task 005 is complete for safe small logo uploads stored as template header data URLs with preview/remove controls and logo source validation.
V6 task 006 is complete for dependency-light server-side PDF generation from published record/report print template versions.
V6 task 007 is complete for trigger email record PDF attachments from published same-form record print templates.
V7 task 001 is complete for backend dashboard analytics contracts, validation, permission-checked execution, hidden-field protection, and frontend API helper coverage.
V7 task 002 is complete for dashboard builder controls over summary, breakdown, trend, and table widgets with V7 analytics previews and compatibility mapping for existing saved dashboard definitions.
V7 task 003 is complete for saved dashboard viewer rendering of V7 analytics widgets, independent per-widget refresh/error states, and denser responsive preview rendering without adding a chart dependency.
V7 task 004 is complete for backend-owned workspace/private dashboard visibility, shared default dashboard metadata, safe legacy dashboard defaults, and dashboard editor controls without a database schema migration.
V8 task 001 is complete: integration API key management now stores only hashed key material, returns raw keys only on create/rotate, tracks active/revoked and last-used metadata, adds `integrations.manage`, writes audit logs for create/revoke/rotate, and registers API-key authentication plumbing without exposing record/report data.
V8 task 002 is complete: integration logs now persist typed inbound/outbound attempt metadata, sanitized request/response metadata, retry state fields, and explicit retry requests with audit logs, without adding background replay.
V8 task 003 is complete: versioned API-key-authenticated record list/read/create endpoints now reuse linked-user form permissions, V3 record scopes, backend record validation, hidden-field filtering, record audit logs, and integration logs.
V8 task 004 is complete: named incoming webhook listeners now persist typed mappings, store listener secrets only as hashes, authenticate inbound calls through API keys or listener secrets, create records through existing validation/permissions, support conservative safe-lookup upserts, and log every inbound attempt.
V8 task 005 is complete: record import jobs now persist CSV import status, explicit field mappings, row-level success/failure results, sanitized validation errors, audit logs, and inbound import integration logs while reusing existing record creation validation and permissions.
V8 task 006 is complete: external export jobs now persist permission-filtered form-record and list-report CSV/JSON artifacts, job status, artifact metadata, audit logs, and outbound export integration logs without public download links.
V8 task 007 is complete: scheduled automation now has explicit daily/weekly/monthly interval/day contracts, tested due-time calculation, stricter unsafe scheduled-action validation, and trigger log schedule metadata for due, locked, skipped, success, and failure runs.
V8 task 008 is complete: scheduled workflow starts now use explicit same-form workflow targets and record selection rules, write workflow history/audit entries, and capture selected record results in trigger logs.
V8 task 009 is complete: `/integrations` now provides a permission-aware operations UI for API key creation/revocation/rotation, webhook listener create/enable/disable/secret rotation, CSV import job creation/status review, export job creation/status/artifact review, sanitized integration log filtering/detail review, and explicit retry requests.
V8 finalization is documented in `docs/V8_FINALIZATION.md`, and practical V8 validation is documented in `docs/V8_PRACTICAL_TESTING.md`; V9 can be postponed and starts from `docs/V9_START_HERE.md` and `tasks/v9/README.md` when needed.

## 2. Core Product Philosophy

Keep the system modular.

Do not build one huge visual builder that controls everything.

The platform should be separated into these modules:

- Form Builder
- Responsive Layout Builder
- Record Engine
- Report Builder
- Permission Engine
- Trigger Engine
- Workflow Builder
- Action Engine
- Print/PDF Engine
- Audit Log Engine
- Notification Engine

Simple mental model:

```txt
Form defines fields.
Layout defines how fields appear.
Record stores submitted values.
Report displays records.
Permission controls who can do what.
Trigger reacts when something changes.
Workflow controls multi-step process.
Action performs safe work for triggers/workflows.
Print template controls printed output.
```

## 3. Important Architecture Decision

Use a custom React responsive builder for form layout.

Do not use XYFlow as the main form layout builder.

XYFlow is good for:

- Workflow diagrams
- Approval flows
- Automation flows
- Conditional process maps
- Visual trigger/workflow builder in later versions

XYFlow is not good for:

- Responsive form layout
- Report table layout
- Permission UI
- Basic trigger UI

Responsive form layout should be based on:

- Page
- Section
- Row
- Column
- Field

The layout should support mobile, tablet, and desktop widths.

Example:

```ts
type Column = {
  id: string;
  span: {
    mobile: number;
    tablet: number;
    desktop: number;
  };
  fields: string[];
};
```

## 4. Core Modules

### 4.1 Form Builder

The Form Builder allows users to create and manage form fields, layout, validation, and publishing.

Supported field types over time:

- Text
- Textarea
- Number
- Email
- Phone
- Date
- Time
- Date range
- Select
- Multi-select
- Radio
- Checkbox
- File upload
- Signature
- Address
- Currency
- User picker
- Department picker
- Status
- Hidden field
- Calculated field

V1 should support only the basic fields.

### 4.2 Responsive Layout Builder

The responsive layout builder should use 12-column grid logic.

The layout model should be:

- Page
- Section
- Row
- Column
- Field

V1 can start with simple width options:

- Full width
- Half width
- One third
- Two thirds

Internally these map to 12-column grid spans:

- Full width = 12
- Half width = 6
- One third = 4
- Two thirds = 8

Mobile should default to full width.

### 4.3 Record Engine

Each submitted form creates a record.

Each record should store:

- Record ID
- Form ID
- Form version
- Submitted values
- Status
- Owner
- Department
- Created by
- Created date
- Updated by
- Updated date
- Audit history

Every record must store the form version used at submission time.

### 4.4 Report Builder

Reports should be separate from forms.

A form collects data. A report displays data.

Report types:

- List report
- Detail report
- Summary report in adjusted V2
- Dashboard/chart lite in adjusted V2
- Advanced analytics later

V1 may have only a default record list.

V2 should add report builder and dashboard-lite features:

- Form data readiness for reporting
- Select columns
- Reorder columns
- Rename columns
- Filters
- Sorting
- Search
- Runnable report viewer
- Real dashboard summaries
- Chart widgets
- Dashboard builder lite
- Print list
- Export CSV
- Report permissions

### 4.5 Printing

There are two print types:

- Print list
- Print single record/detail

V1:

- Browser print from list/detail pages

V2:

- Clean print layouts

Later:

- PDF generation
- Custom print templates
- Conditional print sections
- Branding
- Attach generated PDFs to emails/triggers

### 4.6 Permission Engine

Permission is a major part of the platform.

Access may be controlled by:

- User
- Role
- Group
- Department
- Creator
- Owner
- Manager
- Custom condition

Permission levels over time:

- Application level
- Form level
- Report level
- Record level
- Field level
- Action level
- Trigger level
- Workflow level

Actions include:

- create
- view
- edit
- delete
- print
- export
- approve
- assign
- comment
- change_status
- manage_permissions
- manage_form
- manage_report

V1 permission model should be simple:

- Admin
- Builder
- User
- Viewer

V1 should support:

- Who can submit
- Who can view records
- Who can edit records

Later versions should support:

- Own records only
- Department records only
- Group records only
- Assigned records only
- All records
- Custom rules
- Field-level visibility/read-only/edit access

Important rule:

Permissions must be enforced on the backend/server, not only in the UI.

### 4.7 Trigger Engine

Triggers automate actions after events.

Trigger model:

```txt
When something happens,
if conditions are true,
then run actions.
```

Trigger events over time:

- record.created
- record.updated
- record.deleted
- field.changed
- status.changed
- form.submitted
- record.assigned
- comment.added
- approval.requested
- approval.approved
- approval.rejected
- schedule.daily
- schedule.weekly
- webhook.received

Trigger conditions:

- Field equals value
- Field changed
- Status changed to value
- Amount greater than value
- Department equals value
- User belongs to group
- Date before/after

Trigger actions:

- Send email
- Send notification
- Update field
- Change status
- Assign user
- Assign group
- Create task
- Call webhook
- Generate PDF
- Add comment
- Create related record
- Lock record
- Unlock record
- Start workflow

V4 should add the trigger engine and a small shared action engine foundation.

V1 should not include advanced triggers.

The action engine should start with typed, approved actions such as sending email, creating/updating records, assigning users, adding comments/audit entries, calling APIs/webhooks, and starting workflows. Custom code should only be considered later behind a restricted, auditable execution model.

### 4.8 Workflow Builder

Workflow is different from trigger.

A trigger is usually a small automation. A workflow is a multi-step business process.

Examples:

- Draft
- Submitted
- Manager Review
- Finance Review
- Approved
- Rejected
- Completed

Workflow should support:

- Status states
- Transitions
- Approvers
- Assignment
- Approval/rejection
- Return for correction
- Workflow history
- Actions through the shared action engine

XYFlow is used only for the optional visual workflow builder introduced in V5.

Do not start with full workflow in V1.

Reachable automation order:

1. Build reliable form/record/report data first.
2. Add validation/rule definitions.
3. Add event triggers and approved actions.
4. Add workflow definitions and runner.
5. Add scheduled triggers and richer integration actions.

### 4.9 Audit Logs

Audit logging is required for business use.

Track:

- Record created
- Record updated
- Record deleted
- Record printed
- Report exported
- Permission changed
- Trigger executed
- Workflow transitioned
- Approval/rejection
- User login/security events later

Audit logs should include:

- Entity type
- Entity ID
- Action
- User ID
- Before/after changes where relevant
- Timestamp
- IP/user agent later if needed

### 4.10 Notification System

Notifications should support:

- In-app notifications
- Email notifications
- Later: Slack/Teams/SMS

Triggers and workflows should use the notification system.

## 5. Version Roadmap

### Version 1: Simple Form Builder + Records

Goal: create the foundation.

Features:

- Existing auth integration or simple user model
- Basic roles
- Create form
- Add/edit/delete fields
- Basic field settings
- Simple responsive layout
- Preview form
- Publish form
- Submit form
- Store records
- View record list
- View record detail
- Edit record
- Delete record
- Basic search
- Basic browser print
- Basic permissions
- Basic audit logs
- Seed data

Supported V1 fields:

- Text
- Textarea
- Number
- Email
- Phone
- Date
- Select
- Checkbox
- Radio

Do not include in V1:

- XYFlow
- Advanced workflows
- Complex permissions
- Full report builder
- Custom PDF templates
- Dashboards
- Integrations

### Version 2: Form Data, Reports, Dashboards, Charts, and Better Printing

Goal: turn submitted form data into runnable reports, dashboard summaries, simple charts, exports, and cleaner printed views.

Features:

- Form data readiness for reporting
- Create list report
- Select columns
- Reorder columns
- Rename columns
- Filters
- Sorting
- Search
- Saved reports
- Run saved reports against real records
- Real dashboard summary API
- Chart builder lite
- Dashboard builder lite
- Print list report
- Print record detail with cleaner layout
- Export CSV
- Basic report permissions

### Version 3: Advanced Permissions

Goal: add organization-level access control.

Features:

- Users
- Roles
- Groups
- Departments
- Department managers
- Form-level permissions
- Report-level permissions
- Record-level permissions
- Action-level permissions
- Basic field-level visibility/read-only
- Own records only
- Department records only
- Group records only
- Assigned records only
- Custom rules later

### Version 4: Trigger Engine

Goal: add automation after record changes.

Features:

- Trigger list
- Trigger builder
- On record created
- On record updated
- On field changed
- On status changed
- Backend trigger definitions and logs implemented in V4 task 001
- Send email implemented as a V4 task 001 starter action
- Change status implemented as a V4 task 001 starter action
- Assign user/group implemented as a V4 task 001 starter action
- Trigger management UI implemented in V4 task 002
- Update field implemented in V4 task 003
- Manual failed-log retry implemented in V4 task 004
- Send notification implemented in V4 task 005
- Notification inbox and read state implemented in V4 task 006
- Notification badges and preferences implemented in V4 task 007
- Create record implemented in V4 task 008
- Automatic retry queues implemented in V4 task 009
- Call webhook implemented in V4 task 010
- User-authored retry policies implemented in V4 task 010
- Scheduled triggers for safe actions implemented in V4 task 010

### Version 5: Workflow and Approval System

Goal: support multi-step business processes.

Features:

- Status states
- Status transitions
- Approval steps
- Single approver
- Multiple approvers
- Department manager approval
- Sequential approval
- Parallel approval implemented through V5 task 004 `all` mode for assigned approval tasks
- Workflow history
- Backend workflow definition foundation implemented in V5 task 001
- Workflow management UI implemented in V5 task 002
- Record workflow transition execution implemented in V5 task 003
- Approval inbox and in-app approval notifications implemented in V5 task 004
- Workflow transition action execution implemented in V5 task 005
- Trigger-to-workflow starts implemented in V5 task 006
- Optional XYFlow workflow builder implemented in V5 task 007

### Version 6: Advanced Print Templates and PDF

Goal: professional printable outputs.

Features:

- Persisted custom print templates
- Header/footer
- Logo
- Field values
- Tables
- Signatures
- Browser print/save-as-PDF generation
- Template management UI
- Permission-protected template APIs and audit logs
- Page breaks
- Conditional sections
- Server-side PDF generation
- Attach PDF to email triggers

### Version 7: Advanced Dashboards and Analytics

Goal: deepen the V2 dashboard foundation into richer analytics.

Features:

- Advanced summary reports
- Advanced grouped reports
- Richer charts
- Advanced dashboard builder
- Number cards
- Pending approvals
- Status summaries
- Department summaries

### Version 8: Integrations and API

Goal: connect with external systems.

Features:

- Webhooks
- Public/internal API
- API keys
- Scheduled triggers
- External database sync
- Import records from CSV/Excel
- Export to external systems
- Integration logs
- Retry failed integrations

### Version 9: Enterprise Platform

Goal: complete mature platform.

Features:

- Multi-tenant workspaces
- SSO
- Advanced RBAC/ABAC
- Data retention
- Backup/restore
- White labeling
- Localization
- Custom domains
- Compliance/audit features
- Mobile support
- Offline support optional

## 6. Recommended Tech Stack

Current known stack:

- Frontend: React, React Router, Vite, TypeScript, Tailwind CSS, lucide-react
- Backend: ASP.NET Core minimal APIs targeting .NET 10
- Backend persistence: EF Core with Npgsql
- Database: PostgreSQL 16 through Docker Compose
- Cache/queue foundation: Redis 7 through Docker Compose
- Package manager: npm
- Current frontend runtime requirement: Node.js `>=20.19.0`
- Current frontend tests: Node-based TypeScript logic tests via `npm test`

Recommended additions:

- Frontend language: TypeScript is already used
- Frontend styling: Tailwind CSS and current theme tokens are already used
- Frontend forms: React Hook Form or existing form library
- Frontend validation: Zod or equivalent shared client validation
- Backend validation: FluentValidation or built-in validation
- Backend auth: current cookie auth with bootstrap admin and local users; consider ASP.NET Core Identity, JWT, or external provider only when a later integration task needs it
- Tests: current frontend tests are Node-based TypeScript logic checks; add xUnit/NUnit for backend and Vitest/Jest or React Testing Library when fuller coverage is needed
- Workflow UI: `@xyflow/react` only for the workflow builder

If the existing project already uses different libraries, adapt to it, but keep the architecture modular.

## 7. Suggested Folder Structure

Current frontend root:

```txt
src/app/src/
  components/
  context/
  config/
  layouts/
  pages/
  features/
    dashboards/
    forms/
    notifications/
    records/
    reports/
    triggers/
    workflows/
    users/
  modules/
  platform/
  theme/
  lib/
```

Planned product feature folders should be added under `src/app/src/features/` as their tasks are implemented: permissions, audit, and richer notification/preferences surfaces.

Current frontend shell/theme details:

- `AppShell` is shared by the real app and `/theme` playground.
- Real app routes come from `src/app/src/modules`.
- `/theme` routes come from `src/app/src/theme/config/themePages.tsx`.
- Real app appearance settings are stored under the `appThemeSettings` localStorage key.
- `/theme` playground appearance settings use separate localStorage keys for layout, palette, sidebar state, color mode, density, and top-nav visibility.
- Frontend branding reads `VITE_APP_NAME`, `VITE_COMPANY_NAME`, `VITE_COMPANY_LOGO_URL`, and `BRAND_LOGO_TEXT`.

Current backend root:

```txt
src/api/
  Application/
    Common/
  Domain/
    Common/
    Entities/
  Infrastructure/
    Persistence/
  Modules/
    Dashboard/
    Dashboards/
    Forms/
    Identity/
    Notifications/
    Printing/
    Records/
    Reports/
    Triggers/
    Workflows/
  Platform/
  Configuration/
  Program.cs
```

Planned backend modules should be added under `src/api/Modules/` as their tasks are implemented: audit and richer notification/preferences surfaces.

Current backend configuration details:

- `DotEnv.LoadFromNearestFile()` loads the nearest `.env` file without overriding existing environment variables.
- `EnvironmentConfiguration.ApplyDerivedValues()` maps app, branding, bootstrap admin, connection string, URL, and local CORS environment variables into ASP.NET Core configuration.
- `OpenBusinessPlatformDbContext` maps the V1 database foundation and uses EF Core migrations under `src/api/Infrastructure/Persistence/Migrations`.
- Persisted domain entities use PostgreSQL `uuid` / C# `Guid` IDs, framework-lite audited entity base classes under `src/api/Domain/Common`, and CRUD application primitives under `src/api/Application/Common`.
- `Directory.Build.props` writes API build output under `.artifacts/api`.
- `PermissionService` provides the current global and per-form role checks for auth, Users & Access, forms, records, and report definition endpoints.

Docs and AI task files:

```txt
docs/
tasks/
prompts/
```

## 8. Important Engineering Rules

- Use strong typing.
- Avoid `any` or dynamic objects in frontend unless necessary.
- In backend, avoid passing raw JSON everywhere; use DTOs and typed domain models where practical.
- Keep schema separate from UI.
- Keep business logic outside React components.
- Keep business logic outside API controllers.
- Enforce permissions on the backend.
- Do not rely on frontend-only permission checks.
- Version every published form.
- Store form version on every record.
- Add audit logs for important actions.
- Keep reports separate from forms.
- Keep triggers separate from workflows.
- Start with simple settings UIs before visual builders.
- Do not introduce XYFlow until workflow/approval features.
- Every feature should have acceptance criteria.
- Every task should be small enough for one PR.

## 9. First Implementation Tasks

Start with these V1 tasks:

1. Project inventory and setup
2. Core form types and schemas
3. Database foundation
4. Form list and create page/API
5. Basic field builder
6. Responsive layout builder
7. Form renderer and preview
8. Publish form version
9. Record submission
10. Record list and detail
11. Record edit and delete
12. Basic print
13. Basic permissions
14. Audit log basics
15. Seed data

The V1 foundation is now stable; continue later work through the roadmap/task order and do not skip ahead to triggers or workflow before their versions.

## 10. Codex / AI Instructions

When using Codex or ChatGPT to implement this project:

- Read this file first.
- Read AGENTS.md if available.
- Read the relevant docs file.
- Read the specific task file.
- Implement only the requested task.
- Do not build unrelated features.
- Do not change the database schema unless the task requires it.
- Add or update tests where practical.
- Run lint/typecheck/test commands if available.
- Summarize files changed, tests run, risks, and follow-up tasks.

Preferred prompt:

```txt
Read docs/MASTER_PRD_FOR_AI.md, AGENTS.md, and the selected task file. Implement only this task. Do not build unrelated features. Follow the architecture and roadmap.
```

## 11. Current Priority

The current V1 foundation is complete and verified. Project inventory/setup, shared core form schema work, database foundation, persistent form list/create, backend-owned draft metadata and schema editing, responsive layout, preview, immutable publishing, users/roles access, record submission, record list/detail, record edit/delete, basic print, audit log coverage, and seed data are implemented.
V2 is complete for the current task list: saved list report definitions, column selection/order/custom labels, filters, sort, backend validation, permission-checked persistence, runnable report viewing, CSV export, cleaner print layouts, real dashboard summary data, chart widget previews, and saved dashboard definitions are implemented.
V3 is complete for advanced permissions.
V4 is complete for the current roadmap scope through task 010: trigger engine foundation, trigger UI, update-field actions, manual retry, notification actions/inbox/badges/preferences, create-record actions, automatic retry queues, webhook actions, user-authored retry policies, and scheduled triggers for safe email/webhook actions.
V5 is complete through task 007: workflow definition foundation, workflow management UI, record workflow transition execution, approval inbox and approval notifications, workflow transition action execution, trigger-to-workflow starts, and the optional visual workflow builder.
V6 print template foundation is complete through task 007: template persistence, validation, APIs, `/printing`, selected record/report print layouts, browser print/save-as-PDF generation, page setup, repeated table headers, section page-break controls, conditional sections, immutable published template versions, safe small logo uploads, dependency-light server-side PDF downloads, and trigger email record PDF attachments.

V6 task 003 is complete for field-based conditional print template sections.
V6 task 004 is complete for print template versioning.
V6 task 005 is complete for print template logo uploads.
V6 task 006 is complete for server-side PDF generation.
V6 task 007 is complete for PDF email attachments on record-trigger email actions.
V8 task 001 is complete for hashed integration API keys, conservative typed scopes, management endpoints, audit logs, and backend API-key authentication plumbing.
V8 task 002 is complete for integration log persistence, sanitized metadata, retry metadata, management read endpoints, and auditable explicit retry requests.
V8 task 003 is complete for the public/internal record API foundation.
V8 task 004 is complete for incoming webhook listener persistence, typed field mappings, listener secret hashing, authenticated receive endpoints, record create/upsert execution, and integration logs.
V8 task 005 is complete for CSV record import jobs with explicit field mappings, persisted status, row-level results, audit logs, integration logs, and existing record validation/permissions.
V8 task 006 is complete for external export jobs over permitted form records and list reports with hidden-field filtering, CSV/JSON artifacts, persisted metadata, audit logs, and integration logs.
V8 task 007 is complete for explicit scheduled automation contracts, daily/weekly/monthly due-time calculation, safe scheduled action validation, and scheduled trigger log metadata.
V8 task 008 is complete for scheduled workflow starts with explicit record selection and same-form published workflow validation.
V8 task 009 is complete for the integration operations UI over API keys, webhook listeners, import jobs, export jobs, integration logs, and retry requests.

V1 finalization evidence includes frontend tests/build, backend harness/build, and compose API smoke checks for health, demo admin login, current session, forms list, published form schema rendering, records list, record detail, unauthenticated rejection, and viewer permission denials.

Next concrete work is V9 planning/implementation when the enterprise platform sequence is ready.

Everything else should be designed in a way that does not block future versions, but should not be fully implemented yet.

## 12. Final Product Direction

The final platform should become a modular low-code business platform where organizations can build responsive forms, collect records, create reports, control access, automate actions, manage approvals, print professional documents, and integrate with other systems.

Start simple.

Build the foundation correctly.

Add advanced features version by version.
