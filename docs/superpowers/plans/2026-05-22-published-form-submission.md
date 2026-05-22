# Published Form Submission Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the authenticated V1 UI flow for submitting a published form and storing the record against the form's current immutable version.

**Architecture:** The backend exposes a submit-safe published-form read endpoint from the Forms module, while record creation remains in the existing Records module. The frontend uses the shared form renderer for entry, a small submission helper for state/link behavior, and a new route from the Forms module.

**Tech Stack:** ASP.NET Core minimal APIs, EF Core/Npgsql, React, React Router, TypeScript, Vite, Tailwind CSS, lucide-react, lightweight `.mjs` tests.

---

### Task 1: Backend Published Form Contract And Endpoint

**Files:**
- Modify: `src/api.Tests/Program.cs`
- Modify: `src/api/Modules/Forms/FormManagementContracts.cs`
- Modify: `src/api/Modules/Forms/FormManagementService.cs`
- Modify: `src/api/Modules/Forms/FormsEndpoints.cs`

- [x] **Step 1: Write the failing backend contract test**

Add this after the existing `PublishFormResponse` assertions in `src/api.Tests/Program.cs`:

```csharp
var publishedSubmission = new PublishedFormSubmissionDto(
    sampleDepartmentId,
    "Expense request",
    "Employee reimbursement intake.",
    publishedVersion.Id,
    1,
    publishableSchema);
AssertEqual(publishedVersion.Id, publishedSubmission.CurrentVersionId, "Published submission responses should expose the immutable current version id.");
AssertEqual(1, publishedSubmission.CurrentVersionNumber, "Published submission responses should expose the immutable current version number.");
AssertEqual(publishableSchema, publishedSubmission.Schema, "Published submission responses should expose only the published schema.");
AssertTrue(PlatformPermissions.FormActions.Contains(PlatformPermissions.Form.Submit), "Form actions should include submit access for published form rendering.");
```

- [x] **Step 2: Run the backend harness to verify it fails**

Run: `dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj`

Expected: FAIL because `PublishedFormSubmissionDto` does not exist.

- [x] **Step 3: Add the backend DTO**

Add this record to `src/api/Modules/Forms/FormManagementContracts.cs` after `PublishFormResponse`:

```csharp
public sealed record PublishedFormSubmissionDto(
    Guid Id,
    string Name,
    string? Description,
    Guid CurrentVersionId,
    int CurrentVersionNumber,
    FormSchemaDefinition Schema);
```

- [x] **Step 4: Add the service method**

Add `GetPublishedFormForSubmissionAsync` to `FormManagementService`:

```csharp
public async Task<PublishedFormSubmissionDto> GetPublishedFormForSubmissionAsync(
    Guid formId,
    CancellationToken cancellationToken)
{
    var form = await dbContext.Forms
        .AsNoTracking()
        .Include(candidate => candidate.CurrentVersion)
        .FirstOrDefaultAsync(candidate => candidate.Id == formId && !candidate.IsDeleted, cancellationToken);

    if (form is null)
    {
        throw new FormManagementException(StatusCodes.Status404NotFound, "Form was not found.");
    }

    if (!string.Equals(form.Status, FormStatuses.Published, StringComparison.Ordinal)
        || form.CurrentVersionId is null
        || form.CurrentVersion is null)
    {
        throw new FormManagementException(StatusCodes.Status409Conflict, "Only published forms can be submitted.");
    }

    var schema = DeserializeSchema(form.CurrentVersion.SchemaJson);
    if (schema is null)
    {
        throw new FormManagementException(StatusCodes.Status409Conflict, "Published form version schema is invalid.");
    }

    var validation = FormSchemaValidator.ValidateSchema(schema);
    if (!validation.Valid)
    {
        throw new FormManagementException(StatusCodes.Status409Conflict, "Published form version schema is invalid.", validation.Errors);
    }

    return new PublishedFormSubmissionDto(
        form.Id,
        form.Name,
        form.Description,
        form.CurrentVersion.Id,
        form.CurrentVersion.VersionNumber,
        schema);
}
```

- [x] **Step 5: Add the endpoint**

In `FormsEndpoints.MapFormsEndpoints`, add `GET /api/forms/{formId}/published` before the generic `GET /api/forms/{formId}` route. Use `CanAccessFormAsync` with `PlatformPermissions.Form.Submit`, then return `formManagement.GetPublishedFormForSubmissionAsync(...)` through `HandleFormRequestAsync`.

- [x] **Step 6: Verify backend green**

Run: `dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj`

Expected: PASS.

### Task 2: Frontend Published Form API Client

**Files:**
- Modify: `src/app/src/features/forms/formsApi.test.mjs`
- Modify: `src/app/src/features/forms/api.ts`

- [x] **Step 1: Write the failing API client test**

In `formsApi.test.mjs`, add a fetcher branch for `GET /api/forms/form-2/published` returning:

```js
{
  id: "form-2",
  name: "Safety inspection",
  description: null,
  currentVersionId: "version-1",
  currentVersionNumber: 1,
  schema: JSON.parse(calls[3].init.body).schema
}
```

Call `const publishedSubmissionForm = await api.getPublishedFormForSubmission("form-2", fetcher);` after publishing, and assert:

```js
assert.equal(publishedSubmissionForm.currentVersionId, "version-1");
assert.equal(publishedSubmissionForm.schema.fields[0].id, "site_name");
assert.equal(calls[5].input, "/api/forms/form-2/published");
assert.equal(calls[5].init.method, "GET");
assert.equal(calls[5].init.credentials, "include");
```

Adjust later call indexes by one.

- [x] **Step 2: Run the API client test to verify it fails**

Run from `src/app`: `node src/features/forms/formsApi.test.mjs`

Expected: FAIL because `getPublishedFormForSubmission` is not exported.

- [x] **Step 3: Add the API type and helper**

In `api.ts`, add:

```ts
export type PublishedFormForSubmission = {
  id: string;
  name: string;
  description?: string | null;
  currentVersionId: string;
  currentVersionNumber: number;
  schema: FormSchema;
};

export async function getPublishedFormForSubmission(
  formId: string,
  fetcher: FormsFetcher = defaultFetcher
): Promise<PublishedFormForSubmission> {
  return requestJson<PublishedFormForSubmission>(
    `/api/forms/${encodeURIComponent(formId)}/published`,
    { method: "GET", credentials: "include" },
    fetcher
  );
}
```

- [x] **Step 4: Verify API client green**

Run from `src/app`: `node src/features/forms/formsApi.test.mjs`

Expected: PASS.

### Task 3: Frontend Submission Helpers

**Files:**
- Create: `src/app/src/features/forms/submission.ts`
- Create: `src/app/src/features/forms/formSubmission.test.mjs`

- [x] **Step 1: Write the failing helper test**

Create `formSubmission.test.mjs` that compiles `types.ts`, `renderer.ts`, `api.ts`, and `submission.ts`, then asserts:

```js
assert.deepEqual(submission.createPublishedFormSubmissionValues(form), { site_name: "" });
assert.deepEqual(submission.getSubmissionSuccessLinks(record), {
  recordPath: "/records/record-1",
  recordsPath: "/forms/form-2/records",
  formsPath: "/forms"
});
assert.deepEqual(submission.clearSubmissionFieldErrors(errors, "site_name"), []);
```

- [x] **Step 2: Run the helper test to verify it fails**

Run from `src/app`: `node src/features/forms/formSubmission.test.mjs`

Expected: FAIL because `submission.ts` does not exist.

- [x] **Step 3: Add the helpers**

Create `submission.ts`:

```ts
import type { FormRecord, PublishedFormForSubmission } from "./api";
import { createInitialRecordValues } from "./renderer";
import type { FormRecordValues, ValidationError } from "./types";

export function createPublishedFormSubmissionValues(form: PublishedFormForSubmission): FormRecordValues {
  return createInitialRecordValues(form.schema);
}

export function clearSubmissionFieldErrors(errors: ValidationError[], fieldId: string): ValidationError[] {
  return errors.filter((validationError) => validationError.path !== `values.${fieldId}`);
}

export function getSubmissionSuccessLinks(record: FormRecord) {
  return {
    recordPath: `/records/${record.id}`,
    recordsPath: `/forms/${record.formId}/records`,
    formsPath: "/forms"
  };
}
```

- [x] **Step 4: Verify helper green**

Run from `src/app`: `node src/features/forms/formSubmission.test.mjs`

Expected: PASS.

### Task 4: Submit Page And Routing

**Files:**
- Create: `src/app/src/features/forms/pages/SubmitFormPage.tsx`
- Modify: `src/app/src/features/forms/api.ts`
- Modify: `src/app/src/features/forms/pages/FormsListPage.tsx`
- Modify: `src/app/src/modules/forms/module.tsx`

- [x] **Step 1: Enhance API errors for validation details**

Update `FormsApiError` in `api.ts` to accept `errors: ValidationError[] = []`, import `ValidationError`, and pass parsed `body.errors` into the constructor. This lets the submit page map backend validation failures to `FormRenderer`.

- [x] **Step 2: Add the submit page**

Create `SubmitFormPage.tsx` with these responsibilities:

- load `getPublishedFormForSubmission(resolvedFormId)`
- initialize values with `createPublishedFormSubmissionValues`
- render `FormRenderer`
- validate with `validateRecordValues`
- call `submitRecord`
- show success links from `getSubmissionSuccessLinks`
- show `FormsApiError.errors` inline when present

- [x] **Step 3: Register the route**

Add `{ path: "/forms/:formId/submit", element: <SubmitFormPage />, permission: "menu.forms" }` to `src/app/src/modules/forms/module.tsx`.

- [x] **Step 4: Add Submit links to Forms list**

In `FormsListPage.tsx`, add a `SubmitFormLink` for published forms in desktop and mobile actions. Use a lucide send-style icon and route to `/forms/${formId}/submit`.

- [x] **Step 5: Verify TypeScript through frontend tests**

Run from `src/app`: `npm test`

Expected: PASS.

### Task 5: Documentation And Final Verification

**Files:**
- Modify: `docs/API_SPEC.md`
- Modify: `docs/MVP_CHECKLIST.md`
- Modify: `tasks/v1/009-record-submission.md`

- [x] **Step 1: Document the published form read endpoint**

Add `GET /api/forms/{formId}/published` to `docs/API_SPEC.md` before record submission. Document authentication, submit permission, and the response shape.

- [x] **Step 2: Update V1 status docs**

Mark V1 record submission, submitted value validation, record version storage, and task 009 acceptance criteria as complete. Do not mark unrelated record list/detail or edit/delete tasks complete here.

- [x] **Step 3: Run final verification**

Run:

```bash
cd src/app
npm test
npm run build
cd ../..
dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj
dotnet build src/api/OpenBusinessPlatform.Api.csproj
```

Expected: all commands pass.
