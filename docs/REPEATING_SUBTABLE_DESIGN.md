# Repeating Table / Sub-Table Design

Status: foundation, child-row display, inline child-row create, and grid controls added.

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
3. Add inline child-row create using the child form renderer in a modal.
4. Add child row paging, sorting, and per-column filtering.
5. Add inline child-row edit/delete after the child record update/delete API path is designed and tested.
6. Add child row validation rules such as minimum and maximum rows.
7. Extend print/report views with related child-row sections.

## Permission Rules

Sub-table UI can appear only when the user can view the parent record and has at least view access to the child form records. Inline create, edit, and delete must call child record APIs and pass child form permissions on the backend. Parent form access must not bypass child form access.

## Report Rules

Child forms should automatically get their own default reports. Parent reports should not flatten child rows in the first version. Later report builder work can add related-row counts, summaries, or nested exports explicitly.

## Implemented Foundation

The first code slice adds the schema contract, frontend/backend validation, builder configuration UI, and a disabled/read-only sub-table preview. Sub-table fields are excluded from parent report metadata because they represent related child records, not scalar parent record values.

The second code slice adds a parent-record scoped child-row read API and displays existing child rows in parent record detail and edit views. The API uses the parent record's stored schema version, enforces parent and child form access, applies child record scope filtering, hides unauthorized child fields, and hydrates lookup display labels for visible child columns.

The third code slice adds inline child-row create from the parent record view. The renderer opens the configured child form in a modal, pre-fills the hidden parent lookup with the current parent record ID, validates against the child form schema, submits through the existing child record creation API, and refreshes the related rows after save. The sub-table grid now supports backend paging, sortable column headers, and per-column filters.

Inline child-row edit and delete are intentionally still disabled until the child record update/delete API path is designed and tested.
