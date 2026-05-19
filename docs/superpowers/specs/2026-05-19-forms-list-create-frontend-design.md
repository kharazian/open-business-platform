# Forms List And Create Frontend Design

Date: 2026-05-19

## Goal

Add the first real Forms frontend surface for V1: a `/forms` page where builders can see form drafts and create a new draft form.

This implements the frontend portion of `tasks/v1/004-form-list-and-create.md` while keeping the backend Forms API out of scope until its own task is implemented.

## Scope

Add a Forms module to the real app shell:

- Register a `/forms` route through the platform module registry.
- Add a Forms navigation item.
- Add a list page with search and status filtering.
- Add a create-form modal for name and optional description.
- Store created draft forms in page state for this slice.
- Add focused form draft helper logic and tests where practical.

The page should use existing shared UI primitives from `src/app/src/components/ui` and fit the current real app layout.

## Non-Goals

This slice does not implement:

- Backend Forms endpoints.
- Persistent storage.
- Field builder behavior.
- Responsive layout editing.
- Preview or publishing.
- Record submission, record lists, or record detail.
- Advanced permissions beyond leaving a clear UI/API boundary for later backend checks.

## Architecture

Frontend product code remains under `src/app/src/features/forms`.

The module registration lives under `src/app/src/modules/forms/module.tsx`, matching the existing dashboard/users/reports/settings module pattern. The module exposes a route at `/forms` and a navigation item labeled `Forms`.

The page component lives under `src/app/src/features/forms/pages/FormsListPage.tsx`. It owns only presentation state for this first slice:

- Current search text.
- Current status filter.
- Create modal visibility.
- Local list of draft/sample form summaries.

Shared form schema types stay in `src/app/src/features/forms/types.ts`. Any new list/create types should be explicit TypeScript types and avoid `any`.

## Form Draft Model

The frontend list model should represent only what the V1 list needs:

- `id`
- `name`
- optional `description`
- `status`
- `fieldCount`
- optional `currentVersionId`
- `createdAt`
- `updatedAt`

Statuses are `draft`, `published`, and `archived`, so later API responses can map cleanly onto the same UI.

New drafts should start with an empty V1 schema-compatible draft shell, but this page should not expose editing the schema yet. The field builder task will own schema editing.

## Data Flow

For this slice, local sample data initializes the list page. When the user submits the create modal, the page creates a draft summary in memory and adds it to the list.

The implementation should keep this boundary obvious so a future Forms API client can replace the local state with:

- `GET /api/forms`
- `POST /api/forms`

No fake network layer is needed yet.

## UI Behavior

The `/forms` page should feel like a real product page, not a theme demo:

- Header with page title, short description, and a primary Create button.
- Search input for name or description.
- Status select for all/draft/published/archived.
- Table on desktop.
- Compact stacked rows on mobile if the existing table is too dense.
- Empty state when filters return no forms.
- Create modal with validation for a non-empty name.

Status badges should use existing badge variants. Buttons should use lucide icons where appropriate.

## Error Handling

Since this slice is local-only, error handling is limited to form validation:

- Empty form names should not create drafts.
- The modal should keep user-entered description text while showing the validation message.

Future API errors should be handled in the Forms API client, not directly buried inside the page component.

## Testing

Add focused tests for pure helper logic, such as:

- Creating a draft form summary from input.
- Filtering forms by search and status.
- Rejecting or normalizing empty names.

Run:

- `cd src/app && npm test`
- `cd src/app && npm run build`

## Risks

The main risk is building too much of the form builder before persistence and draft editing exist. Keep this slice intentionally small: list, filter, create draft summary, and route registration only.

Another risk is letting sample state look like a backend contract. Use clear frontend types now, and keep API DTO mapping for the backend Forms task.
