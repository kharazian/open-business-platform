# UI Specification

## Current UI Foundation

The frontend already has a shared app shell and design-system foundation:

- Shared UI components live in `src/app/src/components/ui`.
- Shared layout components live in `src/app/src/components/layout`.
- Real app pages live in `src/app/src/pages`.
- App route/navigation modules live in `src/app/src/modules` and are assembled through `src/app/src/platform/moduleRegistry.ts`.
- `/theme` is a sample-data playground under `src/app/src/theme`.
- Both the real app and `/theme` should use the same shared components and classes.

The `/theme` playground can demonstrate many layouts and pages, but it should not own reusable UI primitives.

Current Forms, Records, Users & Access, V2 Reports, Dashboard summary, Charts, and saved Dashboards pages use real API clients. Settings, profile, and `/theme` pages remain starter or sample surfaces until their product modules are implemented.

Current real app appearance settings live on the Settings page and are stored in browser `localStorage` under `appThemeSettings`. They support palette, light/dark/system mode, density, main app layout, border radius, and shadow. These settings affect the real app shell only; the `/theme` playground has its own layout and appearance controls.

Current `/theme` playground routes cover dashboard, users, roles, permissions, audit logs, notifications, calendar, tasks, billing, documents, reports, settings, profile, forms, tables, utility pages, login, register, forgot/reset password, MFA, layouts, and components.

## Main Navigation

Target navigation:

- Dashboard
- Forms
- Records
- Reports
- Triggers
- Workflows
- Users & Access
- Settings

V1 requires Forms, Records, Users & Access, and basic Settings/Profile shell behavior. Reports, charts, dashboard summaries, and saved dashboards are implemented for the current V2 scope.

Current main app navigation is permission-aware and includes:

- Home/Dashboard
- Forms
- Triggers
- Users
- Reports
- Settings
- Profile
- Theme Playground

As product features are added, replace remaining starter pages with real module pages.

## Trigger Management UI

Current status: V4 task 002 adds a real `/triggers` workspace. It is a form-scoped management surface for the existing backend trigger engine.

The trigger workspace uses:

- Form selection and trigger list.
- Trigger editor with name, description, event, enabled state, condition rows, and action rows.
- Execution log viewer with status, event/entity metadata, timestamps, errors, input JSON, and result JSON.

The builder exposes only the V4 backend-supported events, conditions, and actions. It is intentionally not a diagram surface and does not use XYFlow. Backend permission checks remain authoritative.

## Form Builder Layout

Current status: the V1 builder at `/forms/:formId/builder` is backend-owned. It supports draft metadata editing, adding/editing/deleting fields, responsive width settings, preview, and publishing. The browser may keep local draft state during editing, but the backend draft schema is the source for publishing.

The builder should use three panels:

```txt
Left panel: field palette
Center: responsive form canvas
Right panel: selected field settings
```

Top toolbar:

- Form name
- Save draft
- Preview
- Publish
- Device preview selector

Device preview options:

- Mobile
- Tablet
- Desktop

## Field Palette

V1 field types:

- Text
- Textarea
- Number
- Email
- Phone
- Date
- Select
- Checkbox
- Radio

## Field Settings Panel

V1 settings:

- Label
- Placeholder
- Required
- Help text
- Default value
- Width
- Options for select/radio/checkbox

Later settings:

- Validation rules
- Conditional visibility
- Field permissions
- Calculated expression

## Responsive Layout UI

V1 width options:

- Full width
- Half width
- One third
- Two thirds

Mobile should default to full width.

## Form Preview

Preview renders the form using the same FormRenderer used for real submissions.

## Record List

V1:

- Table of records
- Search
- Status
- Created date
- Created by
- Open detail
- Print current list

V2:

- Configurable columns
- Filters
- Sorting
- Export
- Print

## Record Detail

Show submitted values using the immutable form version schema stored on the record.

Actions:

- Edit
- Delete if allowed
- Print

## Permissions UI

V1 implemented permissions:

- Global role permissions for menu visibility and platform actions
- Per-form role access for submit, view, edit, delete, and manage
- Backend checks for forms, records, users, roles, dashboard, and report definition management

Options:

- Admin only
- Builder
- Users with access
- Everyone authenticated

Later:

- Own records
- Department records
- Group records
- Custom rules
