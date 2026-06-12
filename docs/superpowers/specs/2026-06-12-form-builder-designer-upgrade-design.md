# Form Builder Designer Upgrade Design

## Scope

This slice upgrades the existing backend-owned form builder into a structured drag-and-drop designer. It focuses on form layout authoring only:

- Drag field types from the palette onto the canvas.
- Reorder existing fields.
- Move fields between rows and columns.
- Drag simple layout blocks into the canvas.
- Keep the form renderer, record engine, reports, permissions, triggers, workflows, printing, and dashboards as separate modules.

Default reports, dynamic menu shortcuts, richer field types, conditional visibility, and raw Tailwind class editing are separate follow-up slices.

## Current Foundation

The current builder already uses the right product model:

```txt
Form schema
  Page
    Section
      Row
        Column
          Field
```

The schema stores responsive spans as `mobile`, `tablet`, and `desktop` values from 1 to 12. The V1 builder supports field add/edit/delete, width presets, backend draft saving, local draft recovery, preview, and publishing. The next step should improve the authoring experience without replacing this schema or introducing XYFlow.

## Recommended Approach

Use a structured 12-column drag-and-drop grid, not a free-position canvas.

The designer should have four stable areas:

- Left panel: field palette and layout blocks.
- Center canvas: sections, rows, columns, field cards, and visible drop zones.
- Right panel: selected field, row, column, or section settings.
- Top toolbar: save draft, preview, publish, and mobile/tablet/desktop preview mode.

The first implementation should use controlled Tailwind-style settings instead of raw Tailwind class input. Builders can choose schema-backed layout width controls first. Safe presentation presets and raw or advanced class editing are follow-up work for trusted admins after the drag-and-drop workflow is stable.

## Drag-And-Drop Engine

Use `@dnd-kit/core` and `@dnd-kit/sortable` for the drag-and-drop interaction unless implementation constraints make that impossible. This is a small, proven React drag-and-drop library and is justified because accessible cross-list drag-and-drop, collision detection, keyboard movement, and sortable state are easy to get wrong with native browser APIs.

The implementation should not use XYFlow for form design.

Drag payload types:

- `new_field`: field type dragged from the palette.
- `existing_field`: existing field moved from one layout target to another.
- `layout_block`: section or row block dragged from the palette.

Drop target types:

- Section body: add a row or place a field as a new full-width row.
- Row start/end: place a field before or after row contents.
- Column: place or replace the field target in that column.
- Between rows: insert a row.
- Between sections: insert a section.

Keyboard and click fallbacks should exist for important actions: add field, move field up/down, move field between adjacent columns, add row, add section, and delete block. Drag-and-drop is the primary experience, but it should not be the only way to recover from a layout mistake.

## Layout Blocks

The left panel should include these layout blocks in the first version:

- Section.
- One-column row.
- Two-column row.
- Three-column row.

Row blocks map to 12-column spans:

- One-column row: `[12]`
- Two-column row: `[6, 6]`
- Three-column row: `[4, 4, 4]`

Mobile always resolves to full-width columns. Tablet and desktop use the configured spans.

Draft schemas may contain empty rows or empty columns while a builder is arranging the canvas. The form renderer should ignore empty rows and columns in entry and readonly modes so published forms do not display accidental blank layout cells.

## Canvas Behavior

The center canvas should look and behave like a business form designer, not a marketing page builder.

Expected behavior:

- Empty sections show clear drop targets.
- Rows show their 12-column structure with subtle grid guides.
- Columns show drop zones when empty.
- Field cards show field label, type, required status, and width.
- Selected field, row, column, or section is highlighted.
- Dragging over a valid target shows a clear insertion indicator.
- Invalid drops are ignored and leave the schema unchanged.
- Device preview changes the canvas width rules without changing saved schema.

The designer should keep cards and panels compact, predictable, and work-focused. It should not use decorative oversized hero sections, gradient backgrounds, or free-floating visual effects.

## Settings Panel

The right panel should be context-aware.

Field settings:

- Field type.
- Label.
- Placeholder, when relevant.
- Help text.
- Required.
- Default value.
- Options for select and radio.
- Width preset: full, half, third, two-thirds, or custom 1-12 span.

Section settings:

- Title.
- Description.

Row or column settings:

- Column span controls.
- Quick presets for one, two, or three columns.
- Delete row or column when safe.

For this first implementation, only existing schema-backed settings must persist. Presentation presets such as plain, bordered, or emphasized are excluded from this slice unless the backend schema contracts and validators are updated in the same implementation plan.

## Schema And Data Flow

Keep schemas and builder UI logic separate.

Add pure helper functions in the forms feature for designer operations:

- Create layout blocks.
- Insert a new field at a drop target.
- Move an existing field to a drop target.
- Reorder fields in a row.
- Add, update, and delete sections.
- Add, update, and delete rows.
- Normalize layout after deletes.
- Remove orphaned field references.
- Skip empty layout cells for rendering.

The page component should delegate schema mutations to these helpers instead of embedding layout manipulation in JSX. This keeps the drag-and-drop UI replaceable and makes the risky layout behavior testable without a browser.

Draft saves still use the existing `PUT /api/forms/{formId}` endpoint. Publishing still uses the existing backend publish endpoint and immutable form version behavior.

## Backend Impact

No database migration is required for the drag-and-drop designer if it only uses the existing schema fields and layout spans.

Backend changes are required only if the first implementation persists new section or field appearance settings. If appearance presets are included, update:

- `src/api/Modules/Forms/FormSchemaContracts.cs`
- `src/api/Modules/Forms/FormSchemaValidator.cs`
- frontend `src/app/src/features/forms/types.ts`
- frontend validation and renderer helpers
- API docs showing the new optional schema properties

Backend authorization remains unchanged. Users still need form manage access or `forms.manage_all` to edit a form draft or publish it.

## Error Handling

Designer operations should be forgiving:

- Invalid drag target: leave schema unchanged.
- Missing field referenced by layout: hide it in the canvas and surface a small builder warning.
- Field not placed in layout: show a builder warning and offer to place it at the end of the first section.
- Empty draft: show an empty canvas state and a large first drop target.
- Backend save failure: keep the existing local recovery cache behavior.
- Publish validation failure: show backend validation errors without losing local draft edits.

The builder should never silently delete a real field. Deleting a field remains an explicit action from the settings panel.

## Testing

Frontend tests should cover the pure designer helpers:

- Creating one-, two-, and three-column row blocks.
- Inserting new fields into empty and populated targets.
- Reordering fields without duplicates.
- Moving fields between rows and sections.
- Deleting a field removes layout references.
- Deleting rows or sections preserves fields or moves them to a safe fallback.
- Renderer helpers skip empty layout rows and columns.
- Device span class behavior remains stable.

Component or integration tests can stay lightweight unless the project adds a browser testing stack. The implementation must still run:

```bash
cd src/app
npm test
npm run build
```

If the implementation updates backend schema contracts or validators, also run:

```bash
dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj
dotnet build src/api/OpenBusinessPlatform.Api.csproj
```

## Risks

Drag-and-drop can make schemas inconsistent if layout mutation is scattered through React components. Keep mutations in pure helpers and test them heavily.

Empty layout blocks are useful while drafting, but they can create blank rendered space if the shared renderer does not ignore them.

Raw Tailwind classes are powerful but can break responsive behavior, visual consistency, and safe rendering. Keep first-version styling controlled through schema-backed presets or existing width controls.

Adding a drag-and-drop dependency requires an install step and should be documented in the implementation plan. The dependency is justified only for the designer surface and should not become a general platform requirement outside form builder UI.

## Out Of Scope

- Default report creation.
- Dynamic form/report menu entries.
- Report row click-through improvements.
- New field types.
- Conditional visibility.
- Calculated fields.
- Raw Tailwind class editing.
- Workflow or trigger visual builders.
- XYFlow in the form builder.
- Database schema changes unless appearance presets are included.
