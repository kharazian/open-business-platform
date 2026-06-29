# Repeating Table / Sub-Table Design

Status: first implementation foundation added.

## Decision

Repeating tables should be modeled as child forms and child records, not as embedded arrays inside a parent record JSON value.

This keeps the platform modular:

- A parent form defines the main record.
- A child form defines the repeated row shape.
- Each repeated row is a normal record on the child form.
- A configured parent lookup links each child record back to the parent record.

## Why This Model

Embedded JSON arrays look simpler at first, but they create a second record engine inside one field. That makes permissions, reports, audit logs, workflows, imports, print templates, and lookup labels harder later.

Child records keep repeated data first-class:

- Reports can run directly on child forms.
- Record audit can track each row create/edit/delete.
- Permissions can be enforced with the same form and record rules.
- Workflows/triggers can target child rows later.
- Print templates can render parent records with related child rows later.

## Schema Shape

The parent form can include a `subTable` layout field with configuration like:

```ts
type SubTableConfig = {
  sourceType: "child_form_records";
  childFormId: string;
  parentLookupFieldId: string;
  displayColumnFieldIds: string[];
  allowInlineCreate: boolean;
  allowInlineEdit: boolean;
  allowInlineDelete: boolean;
  minRows?: number;
  maxRows?: number;
};
```

The child form owns its normal fields plus a required `recordLookup` field that points back to the parent form.

## User Experience Phases

1. Configure a sub-table block in the form builder by selecting child form, parent lookup field, and visible columns.
2. Render existing child rows inside parent record detail/edit views.
3. Add inline child-row create/edit/delete using the child form renderer in a modal or drawer.
4. Add child row validation rules such as minimum and maximum rows.
5. Extend print/report views with related child-row sections.

## Permission Rules

Sub-table UI can appear only when the user can view the parent record and has at least view access to the child form records. Inline create, edit, and delete must call child record APIs and pass child form permissions on the backend. Parent form access must not bypass child form access.

## Report Rules

Child forms should automatically get their own default reports. Parent reports should not flatten child rows in the first version. Later report builder work can add related-row counts, summaries, or nested exports explicitly.

## Implemented Foundation

The first code slice adds the schema contract, frontend/backend validation, builder configuration UI, and a disabled/read-only sub-table preview. Sub-table fields are excluded from parent report metadata because they represent related child records, not scalar parent record values.

Inline child-row create, edit, and delete are intentionally still disabled until the parent-child record API path is designed and tested.
