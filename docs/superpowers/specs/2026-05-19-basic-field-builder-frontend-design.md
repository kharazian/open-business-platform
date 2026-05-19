# Basic Field Builder Frontend Design

Date: 2026-05-19

## Goal

Add the V1 frontend form builder surface for `tasks/v1/005-basic-field-builder.md`: builders can open a form draft, add V1 fields, edit basic field settings, delete fields, and save the draft schema locally until backend draft schema persistence is implemented.

## Scope

- Add a real app route at `/forms/:formId/builder`.
- Add builder entry actions from the Forms list page.
- Add a three-panel builder page: field palette, form canvas, selected field settings.
- Support V1 field types: text, textarea, number, email, phone, date, select, checkbox, radio.
- Support basic settings: label, placeholder, required, help text, default value, and options for select/radio.
- Store the edited schema in browser `localStorage`, keyed by form ID.
- Keep builder schema logic in pure helpers with focused tests.

## Non-Goals

- Do not add backend schema persistence endpoints in this frontend slice.
- Do not implement publishing.
- Do not implement record submission, record list, or record detail.
- Do not implement responsive layout builder behavior from `tasks/v1/006`.
- Do not use XYFlow.
- Do not add advanced validation rules or conditional visibility.

## Architecture

Pure form-builder schema operations live in `src/app/src/features/forms/builder.ts`. The helper owns field creation, schema insertion, field updates, deletion, option normalization, and `localStorage` draft serialization. This keeps business logic outside React and makes the risky bits testable.

The UI page lives in `src/app/src/features/forms/pages/FormBuilderPage.tsx`. It uses the existing `listForms` API to display the selected form name and local builder helpers for schema edits. The page owns only interaction state: selected field, save notice, and API loading/error state.

Routing stays in `src/app/src/modules/forms/module.tsx`. The Forms list links each form to `/forms/:formId/builder`.

## Data Flow

When the builder opens:

1. Read `formId` from the route.
2. Fetch the forms list and find the selected form summary.
3. Load any local schema draft for the form ID.
4. If none exists, initialize an empty V1 schema with one page and one section.

When a builder adds a field:

1. Create a unique field ID from the label/type.
2. Add the field to the schema.
3. Add a default full-width layout row containing that field.
4. Select the new field.

When a builder saves:

1. Serialize the schema to localStorage under the form ID.
2. Show a short saved notice.

## Error Handling

- If loading the form name fails, show an alert but keep the local builder usable.
- If no form summary matches the route ID, show the raw ID in the title.
- Blank labels normalize back to the field type label.
- Choice fields always keep at least one option.
- Corrupt local storage drafts are ignored and replaced with an empty schema.

## Testing

Add pure helper tests covering:

- Empty schema creation.
- Adding fields with unique IDs.
- Adding choice fields with default options.
- Updating field settings and options.
- Deleting fields from both schema fields and layout placement.
- Local storage serialization fallback behavior.

Run:

- `cd src/app && npm test`
- `cd src/app && npm run build`

## Risks

The main risk is users assuming Save draft persists to PostgreSQL. The UI should label this as a local frontend draft until the backend draft-edit API is implemented.
