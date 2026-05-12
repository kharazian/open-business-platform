# UI Specification

## Current UI Foundation

The frontend already has a shared app shell and design-system foundation:

- Shared UI components live in `src/app/src/components/ui`.
- Shared layout components live in `src/app/src/components/layout`.
- Real app pages live in `src/app/src/pages`.
- `/theme` is a sample-data playground under `src/app/src/theme`.
- Both the real app and `/theme` should use the same shared components and classes.

The `/theme` playground can demonstrate many layouts and pages, but it should not own reusable UI primitives.

## Main Navigation

Suggested navigation:

- Dashboard
- Forms
- Records
- Reports
- Triggers
- Workflows
- Users & Access
- Settings

For V1, only Forms, Records, and basic Settings may be needed.

Current main app navigation is still a starter shell:

- Home/Dashboard
- Users
- Reports
- Settings
- Profile
- Theme Playground

As product features are added, replace starter pages with real module pages.

## Form Builder Layout

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

Preview should render the form using the same FormRenderer used for real submissions.

## Record List

V1:

- Table of records
- Search
- Status
- Created date
- Created by
- Open detail

V2:

- Configurable columns
- Filters
- Sorting
- Export
- Print

## Record Detail

Show submitted values using the form layout.

Actions:

- Edit
- Delete if allowed
- Print

## Permissions UI

V1 simple permissions:

- Who can submit?
- Who can view records?
- Who can edit records?

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
