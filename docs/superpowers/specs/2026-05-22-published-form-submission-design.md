# Published Form Submission Design

## Goal

Add the V1 authenticated flow for submitting a published form from the React app and storing the record against the form's current immutable published version.

## Scope

- Add an authenticated in-app submit page at `/forms/:formId/submit`.
- Render only the current published form schema, not the editable backend draft.
- Submit values through the existing record submission API.
- Keep public anonymous form links, multi-page form navigation, trigger execution, and notification behavior out of scope.

## Backend Design

The backend will expose a focused published-form read endpoint under the Forms module: `GET /api/forms/{formId}/published`. The endpoint will require authentication and per-form submit access through `PermissionService.CanAccessFormAsync(user, formId, PlatformPermissions.Form.Submit, ...)`.

The response will include the minimal data the submit UI needs:

- form id
- form name
- optional description
- current version id
- current version number
- published schema

Draft forms, archived forms, deleted forms, forms without `CurrentVersion`, and forms with invalid published schemas will return an error instead of leaking draft schema details. Record creation remains owned by `POST /api/forms/{formId}/records`, which already validates values against the current published version, stores `records.form_version_id`, and writes `record_created`.

## Frontend Design

The frontend will add `SubmitFormPage` under `src/app/src/features/forms/pages`. It will load the published form using a new API client helper, initialize empty record values with the shared renderer helper, render the shared `FormRenderer`, validate client-side with `validateRecordValues`, and call `submitRecord`.

After a successful submit, the page will show a success state with the created record id and links to record detail, records list, and forms. The page will preserve validation errors inline through the existing renderer error model.

The Forms list will show a `Submit` action for published forms alongside existing Build and Records actions. Draft-only forms will keep Submit disabled or hidden so users do not land on an expected backend conflict as the normal path.

## Routing And Permissions

The forms module will register `/forms/:formId/submit` behind the existing `menu.forms` frontend gate. Backend authorization remains the source of truth through form submit permission on the published-read endpoint and the existing record submission endpoint.

## Error Handling

- Loading errors show an `Alert` on the submit page.
- Backend validation errors are mapped to the form renderer where possible.
- Non-field API errors remain visible as page-level alerts.
- Draft/unpublished forms do not render a submit form.

## Tests

Tests will follow the current lightweight test style:

- Extend `formsApi.test.mjs` with the published form client helper and request shape.
- Add a small submission helper test for submit state initialization and successful result links.
- Extend the backend harness with DTO/service type checks for the published-form response.
- Run frontend tests/build and backend harness/build after implementation.
