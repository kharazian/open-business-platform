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
- Future PDF generation, dashboards, and integrations

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
- `src/app/src/features/workflows`: current V5 workflow management API client, builder helpers, typed definition/record execution contracts, and form-scoped workflow management page for definition draft/edit/publish/enable/disable operations
- `src/app/src/features/notifications`: current V4 notification inbox API client/types/page for current-user notifications, read state, unread badges, and preferences
- `src/app/src/context/AuthContext.tsx`: cookie-auth session state and effective frontend permissions
- `src/api/Modules/Forms`: shared V1 form schema contracts, backend validation, forms list/create/draft/publish endpoints, submit-safe published form endpoint, and form access options for role permission setup
- `src/api/Modules/Records`: record submission, list/detail, edit, soft-delete, backend value validation, permission checks, and audit logging
- `src/api/Modules/Reports`: current V2 list report definition, execution, and CSV export endpoints, config validation, permission checks, and report audit logging
- `src/api/Infrastructure/Persistence/DemoDataSeeder.cs`: development startup seed data for demo users, roles, departments, a published sample form, permissions, and records
- `src/api/Modules/Identity`: bootstrap-admin fallback, local user login, self-service password recovery email for persistent users, users/roles/groups/departments management endpoints, password hashing, scoped permission service, and field access service
- `src/api/Modules/Notifications`: current email sender abstraction for password recovery plus current-user notification inbox APIs/read-state/preference service
- `src/api/Modules/Triggers`: current V4 backend trigger definitions, validation, detail/list/create/update APIs, event dispatch, starter action execution, trigger logs, manual failed-log retry recovery, automatic retry queue worker, in-app notification trigger action, webhook action, user-authored retry policy handling, and scheduled trigger worker
- `src/api/Modules/Workflows`: current V5 backend workflow definitions, typed validation, list/create/read/update/publish/enable/disable APIs, immutable version publishing, record workflow state/start/direct transition execution, and workflow history writes
- `src/api/Modules/Dashboard`: current database-backed dashboard summary API and chart widget preview module
- `src/api/Modules/Dashboards`: current V2 saved dashboard definition API with config/layout validation, permission checks, and audit logging
- `src/api/Infrastructure/Persistence`: EF Core/Npgsql DbContext and migrations for users, password reset tokens, roles, groups, departments, role permissions, scoped form permissions, report permissions, field permissions, forms, form versions, records, audit logs, current V2 report definitions, saved dashboard definitions, V4 trigger definitions, trigger logs, automatic trigger retry metadata, V4 trigger schedule/retry policy metadata, V5 workflow definitions/versions/history, notifications, and notification preferences
- `src/app/src/context/AppThemeContext.tsx`: real app appearance settings saved in browser `localStorage`
- `src/app/src/context/ThemeAppearanceContext.tsx`: separate `/theme` playground appearance settings
- `src/app/src/theme`: sample-data theme playground
- `docker-compose.yml`: PostgreSQL and Redis
- `npm test` in `src/app`: lightweight TypeScript logic tests for module registry, form schema/records, forms API/list/builder/submission helpers, auth, users API/types, record edit/print helpers, trigger and workflow API/builder helpers, and shared UI helpers

Treat settings/profile pages as starter UI. Forms, Users & Access, records, record-level permissions, browser print, startup demo data, password recovery email for persistent users, and core audit logs are the finalized V1 baseline. V2 is complete for the current task list: saved list report definitions, column ordering, custom column labels, runnable report viewing, CSV export, chart previews, saved dashboard layouts, cleaner print layouts, and real database-backed dashboard summaries are implemented. V3 is complete for the current task list: groups, departments, department managers, scoped record access, report permissions, action permissions, assignment/status record actions, and basic field hidden/read-only rules are implemented. V4 task 001 is complete for the backend trigger foundation: trigger definitions, trigger logs, management APIs, record event dispatch, supported conditions, audit/email/status/assignment starter actions, and non-recursive action execution. V4 task 002 is complete for the trigger management UI: form-scoped trigger list, builder, enable/disable editing, and logs viewer. V4 task 003 is complete for current-record `update_field` trigger actions. V4 task 004 is complete for manual retry recovery of failed trigger logs. V4 task 005 is complete for database-backed in-app notification trigger actions. V4 task 006 is complete for current-user notification inbox APIs, unread counts, read state, and the real `/notifications` page. V4 task 007 is complete for unread badges and current-user notification preferences. V4 task 008 is complete for `create_record` trigger actions that create one related record in a published target form from literal values and source-field references without recursive trigger dispatch. V4 task 009 is complete for automatic retry queue metadata, hosted retry processing, retry log linkage, and trigger-log retry state display. V4 task 010 is complete for `call_webhook`, user-authored retry policy controls, and scheduled triggers for safe email/webhook actions. V5 task 001 is complete for backend workflow definition persistence, typed validation, management APIs, publish/version contracts, audit logs, and workflow history foundation. V5 task 002 is complete for the first workflow management UI: `/workflows` can list form-scoped workflow definitions, edit JSON-backed draft configs, publish versions, and enable or disable saved definitions. V5 task 003 is complete for record workflow state, published workflow starts, direct transition execution, record status updates, workflow history, audit logs, and record detail controls. Incoming webhook listeners, approval inboxes, workflow notifications, workflow action execution, trigger-to-workflow starts, XYFlow, and workspace ownership for dashboards remain later modules. The settings page currently persists real app appearance preferences only; it does not persist workspace settings to the backend. Build product modules through the task files under `tasks/`.

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

XYFlow can be used here in later versions.

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
- Parallel approval later
- Workflow history
- Backend workflow definition foundation implemented in V5 task 001
- Workflow management UI implemented in V5 task 002
- Record workflow transition execution implemented in V5 task 003
- Optional XYFlow workflow builder

### Version 6: Advanced Print Templates and PDF

Goal: professional printable outputs.

Features:

- Custom print templates
- Header/footer
- Logo
- Field values
- Tables
- Signatures
- Page breaks
- Conditional sections
- PDF generation
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
- Future workflow UI: `@xyflow/react` only for workflow builder

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
    Records/
    Reports/
    Triggers/
  Platform/
  Configuration/
  Program.cs
```

Planned backend modules should be added under `src/api/Modules/` as their tasks are implemented: workflows, printing, audit, and richer notification/preferences surfaces.

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

The current V1 foundation is complete and verified. Project inventory/setup, shared core form schema work, database foundation, persistent form list/create, backend-owned draft metadata and schema editing, responsive layout, preview, immutable publishing, users/roles access, record submission, record list/detail, record edit/delete, basic print, audit log coverage, and seed data are implemented. V2 is complete for the current task list: saved list report definitions, column selection/order/custom labels, filters, sort, backend validation, permission-checked persistence, runnable report viewing, CSV export, cleaner print layouts, real dashboard summary data, chart widget previews, and saved dashboard definitions are implemented. V3 is complete for advanced permissions. V4 is complete for the current roadmap scope through task 010: trigger engine foundation, trigger UI, update-field actions, manual retry, notification actions/inbox/badges/preferences, create-record actions, automatic retry queues, webhook actions, user-authored retry policies, and scheduled triggers for safe email/webhook actions. V5 task 001 is complete for backend workflow definition foundation, V5 task 002 is complete for workflow management UI, and V5 task 003 is complete for record workflow transition execution.

V1 finalization evidence includes frontend tests/build, backend harness/build, and compose API smoke checks for health, demo admin login, current session, forms list, published form schema rendering, records list, record detail, unauthenticated rejection, and viewer permission denials.

Next concrete work should continue with approval inbox and workflow notification groundwork.

Everything else should be designed in a way that does not block future versions, but should not be fully implemented yet.

## 12. Final Product Direction

The final platform should become a modular low-code business platform where organizations can build responsive forms, collect records, create reports, control access, automate actions, manage approvals, print professional documents, and integrate with other systems.

Start simple.

Build the foundation correctly.

Add advanced features version by version.
