# V2 Form Data Readiness Design

## Status

Approved for implementation as the next V2 slice.

## Goal

Prepare form schemas and record values for reliable report viewing, report printing, dashboard summaries, validation rules, triggers, workflows, schedules, and future actions without changing persistence or published form versions.

## Scope

This slice adds shared reportable field metadata helpers in the frontend and backend.

The helpers normalize:

- Form field IDs, labels, types, and source.
- Choice option labels and values for select/radio fields.
- System fields used by reports and future automation: record status, created date, created by, updated date, updated by, owner, and department.
- Field capabilities needed by reports and later charts/rules: searchable, sortable, filterable, numeric aggregation support, and choice-grouping support.

This slice also updates existing list report config building and validation to consume the shared metadata helpers instead of each layer inventing its own field list.

## Non-Goals

- No report execution endpoint.
- No dashboard summary implementation.
- No chart builder.
- No CSV export.
- No PDF/template builder.
- No trigger engine.
- No workflow engine.
- No scheduled runner.
- No general action engine.
- No database migration.
- No mutation of existing `FormVersion.SchemaJson` data.

## Architecture

### Backend

Add a focused form metadata helper under `src/api/Modules/Forms`.

The backend helper exposes immutable reportable field metadata derived from `FormSchemaDefinition` plus system fields. Reports can validate columns, filters, and sorts against that helper. Later modules can reuse it for report execution, dashboard aggregation, validation rules, triggers, and workflows.

The metadata helper should not depend on reports. Forms own field definitions; reports consume them.

### Frontend

Add a matching helper under `src/app/src/features/forms`.

The frontend helper exposes reportable field metadata derived from `FormSchema` plus system fields. The existing report builder helper will consume this metadata and keep its current page-facing behavior.

### Data Flow

1. Form schema is loaded from draft/current form detail or from published form version data.
2. Helper derives reportable fields.
3. Report builder uses fields to offer columns, filters, and sort choices.
4. Backend report validation uses the same field IDs and capability rules.
5. Later report execution and automation read from the same metadata model.

## Field Model

Each reportable field should expose:

- `id`
- `label`
- `type`
- `source`: `form` or `system`
- `options` for choice fields
- `filterable`
- `sortable`
- `searchable`
- `supportsAggregation`
- `supportsChoiceGrouping`

Initial system fields:

- `status`: Record status, text-like, filterable/searchable/sortable/groupable.
- `created_at`: Created date, date-like, filterable/sortable.
- `created_by_id`: Created by, user-like ID, filterable/sortable.
- `updated_at`: Updated date, date-like, filterable/sortable.
- `updated_by_id`: Updated by, user-like ID, filterable/sortable.
- `owner_id`: Owner, user-like ID, filterable/sortable.
- `department_id`: Department, department-like ID, filterable/sortable/groupable.

## Compatibility

Existing report configs that use `status`, `created_at`, and `created_by_id` remain valid.

Existing V1 form schema, record validation, record submission, record detail, record edit, and browser print behavior must continue to pass.

No existing published form version data is rewritten.

## Testing

Backend executable harness should assert:

- Backend reportable metadata includes form fields and system fields.
- Choice field options preserve labels and values.
- Numeric fields support aggregation.
- Choice fields support choice grouping.
- System fields include `updated_at`, `updated_by_id`, `owner_id`, and `department_id`.
- List report config validation accepts known system fields and rejects unknown fields.

Frontend Vitest tests should assert:

- Frontend reportable metadata includes form fields and system fields.
- Choice field options preserve labels and values.
- Report builder field options keep the current `id`, `label`, and `source` behavior.
- List report config creation uses metadata labels and default widths.

Full verification:

- `cd src/app && npm test`
- `cd src/app && npm run build`
- `dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj`
- `dotnet build src/api/OpenBusinessPlatform.Api.csproj`

## Completion Criteria

- `tasks/v2/005-form-data-readiness.md` acceptance criteria are marked complete.
- Existing report builder behavior still works.
- Backend report config validation uses shared metadata.
- Frontend report builder uses shared metadata.
- No persistence migration is introduced.
