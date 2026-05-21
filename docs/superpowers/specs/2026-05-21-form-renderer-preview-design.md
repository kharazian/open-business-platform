# Form Renderer and Preview Design

## Goal

Implement task `tasks/v1/007-form-renderer-and-preview.md` by adding a reusable frontend form renderer and a builder preview flow. Preview must render the current draft schema/layout through the same component that later published form submission and record detail pages can reuse.

## Scope

- Add a shared V1 `FormRenderer` component in the forms feature area.
- Render the current `FormSchema` layout model: pages, sections, rows, columns, and fields.
- Support all current V1 field types: text, textarea, number, email, phone, date, select, checkbox, and radio.
- Add a preview action to the existing form builder page.
- Validate preview values locally with existing record-value validation.
- Add focused frontend tests for renderer helper logic.
- Update task/checklist documentation after implementation.

Out of scope:

- Backend draft schema persistence.
- Publishing immutable form versions.
- Creating records from preview submissions.
- Record submission routes or APIs.
- New permission rules.
- XYFlow or drag-and-drop layout changes.

## Architecture

The renderer belongs under `src/app/src/features/forms/components/` because it is a product-level forms component, not a global UI primitive. The builder page may import it, and later record submission/detail pages may import the same component.

The builder remains responsible for editing draft schema state. The renderer is responsible only for rendering schema/layout and raising value changes/submission events.

Planned units:

- `FormRenderer.tsx`: React component that renders schema layout and V1 field controls.
- `renderer.ts`: small pure helpers for default values, value coercion, layout field lookup, validation error mapping, and forced preview layout class selection.
- `formRenderer.test.mjs`: focused Node-based tests that compile the pure helpers and verify their behavior.

## Component Behavior

`FormRenderer` accepts:

- `schema: FormSchema`
- `values: FormRecordValues`
- `errors?: ValidationError[]`
- `mode?: "entry" | "readonly"`
- `previewSize?: "responsive" | "mobile" | "tablet" | "desktop"`
- `submitLabel?: string`
- `onChange?: (fieldId: string, value: FormRecordValue) => void`
- `onSubmit?: () => void`

Entry mode renders editable controls. Read-only mode is reserved for later record detail work and can render disabled controls or value text without changing the public schema contract. For this task, builder preview uses entry mode.

The renderer walks `schema.layout.pages`, then sections, rows, columns, and column field IDs. It resolves fields from `schema.fields`. Unknown field IDs are ignored in rendering because schema validation owns that error state; this keeps the renderer resilient while editing local drafts.

## Layout

The renderer must support the existing 12-column layout spans:

- Mobile defaults to one column.
- Tablet and desktop use column spans from the schema.
- `previewSize="responsive"` uses normal responsive classes.
- `previewSize="mobile" | "tablet" | "desktop"` forces the layout for the preview frame independent of the actual viewport width.

This forced preview mode avoids a common problem where a desktop browser makes a small preview panel still behave like desktop layout.

## Field Rendering

V1 controls map as follows:

- `text`, `email`, `phone`, `date`, `number`: shared `Input`
- `textarea`: shared `Textarea`
- `select`: shared `Select`
- `checkbox`: shared `Checkbox`
- `radio`: option list using native radio inputs styled consistently with existing UI components

Field labels, required markers, placeholder text, help text, and validation errors are displayed consistently. Number input values are converted to numbers before validation, blank values become `null`, checkbox values become booleans, and other field values remain strings.

## Builder Preview Flow

The builder toolbar gets a `Preview` button. Opening preview shows a large modal with:

- Device selector: mobile, tablet, desktop.
- Current draft rendered through `FormRenderer`.
- Local submit action labeled `Validate preview`.

Preview state is initialized from schema defaults. Submitting preview calls `validateRecordValues(schema, values)` and shows either field-level errors or a success notice. It does not persist records or call the backend.

If the schema has no fields, preview shows the renderer empty state and the validate action remains harmless.

## Error Handling

Schema validation errors are not added to the preview flow in this task because the existing builder already maintains schema structure and task `008` will handle publish-time schema validation more directly. Preview submission focuses on record-value validation for required fields, email/date/number typing, checkbox booleans, and option membership.

Local API authorization is unchanged. The builder route already requires `menu.forms`; preview is only a UI state inside that protected page.

## Testing

Add tests for pure renderer helpers:

- Initial values use field defaults.
- Checkbox default becomes boolean.
- Number changes coerce valid numbers and blanks to `null`.
- Error mapping returns field-specific errors.
- Forced layout column classes match mobile, tablet, desktop, and responsive behavior.

Existing frontend tests and build should run after implementation:

- `npm test`
- `npm run build`

Backend tests/builds are not required for this task unless frontend changes unexpectedly touch backend contracts.

## Documentation

After implementation, update:

- `tasks/v1/007-form-renderer-and-preview.md`
- `docs/MVP_CHECKLIST.md`

No architecture or command documentation changes are expected.

## Risks

- The existing builder file is already large. The renderer extraction should reduce duplication where practical, but this task should not refactor unrelated builder logic.
- Drafts are still local-only, so preview reflects local draft state rather than a persisted backend draft. This is acceptable until backend draft editing/publishing tasks are implemented.
- Read-only rendering can remain minimal in this slice as long as the public renderer shape can support later record detail reuse.
