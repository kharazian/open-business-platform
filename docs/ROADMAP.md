# Roadmap

## Current State: Skeleton and Theme Foundation

- ASP.NET Core API host exists.
- React frontend shell exists.
- Shared UI/layout components exist.
- `/theme` playground exists for sample-data design review.
- PostgreSQL and Redis run through Docker Compose.

Next: finish project inventory/setup, then implement V1 tasks in order.

## V1: Foundation - Forms and Records

Goal: create a working product foundation.

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

## V2: Reports and Better Printing

Goal: allow users to define useful record views.

Features:

- List report builder
- Column selection
- Column ordering
- Filters
- Sorting
- Search
- Saved reports
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

## V7: Dashboards and Analytics

Goal: management-level summaries.

Features:

- Summary reports
- Charts
- Dashboard builder
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
