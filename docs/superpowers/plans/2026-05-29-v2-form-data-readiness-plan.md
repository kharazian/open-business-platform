# V2 Form Data Readiness Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add shared frontend/backend reportable field metadata helpers so reports, print, dashboards, validation rules, triggers, workflows, and future actions can understand form fields and system fields consistently.

**Architecture:** Forms own reportable metadata because forms own field schemas. Reports consume that metadata for builder options and backend config validation. This slice does not add persistence, report execution, dashboards, triggers, workflows, schedules, or action execution.

**Tech Stack:** ASP.NET Core minimal APIs, C# records/static helpers, React/Vite TypeScript, Vitest, executable backend harness.

---

## File Map

- Create `src/api/Modules/Forms/FormReportableFieldMetadata.cs`
  - Backend metadata records, system field constants, and helper functions.
- Modify `src/api/Modules/Reports/ListReportConfigValidator.cs`
  - Validate report config against backend metadata instead of hand-built raw IDs.
- Modify `src/api/Modules/Reports/ReportManagementContracts.cs`
  - Keep existing `ReportSystemFields` constants compatible by delegating to form metadata constants.
- Modify `src/api.Tests/Program.cs`
  - Add backend harness assertions for metadata and report validation behavior.
- Create `src/app/src/features/forms/reportableFields.ts`
  - Frontend metadata types, system fields, and helper functions.
- Create `src/app/src/features/forms/reportableFields.test.mjs`
  - Vitest coverage for metadata normalization.
- Modify `src/app/src/features/reports/builder.ts`
  - Use frontend form metadata helper.
- Modify `src/app/src/features/reports/types.ts`
  - Keep `reportSystemFields` exported from the metadata helper for compatibility.
- Modify `src/app/src/features/reports/reportsApi.test.mjs`
  - Add report builder assertions that preserve existing field option behavior and cover the new metadata fields.
- Modify `tasks/v2/005-form-data-readiness.md`
  - Mark acceptance criteria complete.

---

### Task 1: Backend Reportable Field Metadata

**Files:**
- Create: `src/api/Modules/Forms/FormReportableFieldMetadata.cs`
- Modify: `src/api.Tests/Program.cs`

- [ ] **Step 1: Add failing backend harness assertions**

Add assertions near the existing `publishableSchema` and report config assertions:

```csharp
var reportingSchema = publishableSchema with
{
    Fields = new FormFieldDefinition[]
    {
        new("employee_name", FormFieldTypes.Text, "Employee name", Required: true),
        new("salary", FormFieldTypes.Number, "Salary"),
        new(
            "department",
            FormFieldTypes.Select,
            "Department",
            Options: new[]
            {
                new FormFieldOptionDefinition("opt_hr", "Human Resources", "hr"),
                new FormFieldOptionDefinition("opt_finance", "Finance", "finance")
            })
    }
};
var reportableFields = FormReportableFieldMetadata.GetReportableFields(reportingSchema);
AssertTrue(reportableFields.Any(field => field.Id == "employee_name" && field.Label == "Employee name" && field.Source == ReportableFieldSources.Form), "Reportable metadata should include form text fields.");
AssertTrue(reportableFields.Any(field => field.Id == "salary" && field.SupportsAggregation), "Reportable metadata should mark number fields as aggregatable.");
AssertTrue(reportableFields.Any(field => field.Id == "department" && field.SupportsChoiceGrouping), "Reportable metadata should mark choice fields as groupable.");
AssertEqual("Human Resources", reportableFields.Single(field => field.Id == "department").Options.Single(option => option.Value == "hr").Label, "Reportable metadata should preserve option labels.");
AssertTrue(reportableFields.Any(field => field.Id == ReportableSystemFields.UpdatedAt), "Reportable metadata should include updated date system field.");
AssertTrue(reportableFields.Any(field => field.Id == ReportableSystemFields.OwnerId), "Reportable metadata should include owner system field.");
AssertTrue(reportableFields.Any(field => field.Id == ReportableSystemFields.DepartmentId), "Reportable metadata should include department system field.");
```

- [ ] **Step 2: Run backend harness and verify it fails**

Run:

```bash
dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj
```

Expected: compile failure because `FormReportableFieldMetadata`, `ReportableFieldSources`, and `ReportableSystemFields` do not exist.

- [ ] **Step 3: Create backend metadata helper**

Create `src/api/Modules/Forms/FormReportableFieldMetadata.cs` with:

```csharp
namespace OpenBusinessPlatform.Api.Modules.Forms;

public static class ReportableFieldSources
{
    public const string Form = "form";
    public const string System = "system";
}

public static class ReportableSystemFields
{
    public const string Status = "status";
    public const string CreatedAt = "created_at";
    public const string CreatedById = "created_by_id";
    public const string UpdatedAt = "updated_at";
    public const string UpdatedById = "updated_by_id";
    public const string OwnerId = "owner_id";
    public const string DepartmentId = "department_id";
}

public sealed record ReportableFieldOptionMetadata(string Id, string Label, string Value);

public sealed record ReportableFieldMetadata(
    string Id,
    string Label,
    string Type,
    string Source,
    IReadOnlyList<ReportableFieldOptionMetadata> Options,
    bool Filterable,
    bool Sortable,
    bool Searchable,
    bool SupportsAggregation,
    bool SupportsChoiceGrouping);

public static class FormReportableFieldMetadata
{
    public static IReadOnlyList<ReportableFieldMetadata> SystemFields { get; } =
    [
        new(ReportableSystemFields.Status, "Record status", "status", ReportableFieldSources.System, Array.Empty<ReportableFieldOptionMetadata>(), true, true, true, false, true),
        new(ReportableSystemFields.CreatedAt, "Created date", "datetime", ReportableFieldSources.System, Array.Empty<ReportableFieldOptionMetadata>(), true, true, false, false, false),
        new(ReportableSystemFields.CreatedById, "Created by", "user", ReportableFieldSources.System, Array.Empty<ReportableFieldOptionMetadata>(), true, true, false, false, false),
        new(ReportableSystemFields.UpdatedAt, "Updated date", "datetime", ReportableFieldSources.System, Array.Empty<ReportableFieldOptionMetadata>(), true, true, false, false, false),
        new(ReportableSystemFields.UpdatedById, "Updated by", "user", ReportableFieldSources.System, Array.Empty<ReportableFieldOptionMetadata>(), true, true, false, false, false),
        new(ReportableSystemFields.OwnerId, "Owner", "user", ReportableFieldSources.System, Array.Empty<ReportableFieldOptionMetadata>(), true, true, false, false, false),
        new(ReportableSystemFields.DepartmentId, "Department", "department", ReportableFieldSources.System, Array.Empty<ReportableFieldOptionMetadata>(), true, true, false, false, true)
    ];

    public static IReadOnlyList<ReportableFieldMetadata> GetReportableFields(FormSchemaDefinition schema)
    {
        return schema.Fields.Select(ToReportableField).Concat(SystemFields).ToArray();
    }

    public static IReadOnlyDictionary<string, ReportableFieldMetadata> GetReportableFieldsById(FormSchemaDefinition schema)
    {
        return GetReportableFields(schema).ToDictionary(field => field.Id, StringComparer.Ordinal);
    }

    private static ReportableFieldMetadata ToReportableField(FormFieldDefinition field)
    {
        var isChoice = FormFieldTypes.IsChoice(field.Type);
        return new ReportableFieldMetadata(
            field.Id,
            field.Label,
            field.Type,
            ReportableFieldSources.Form,
            (field.Options ?? Array.Empty<FormFieldOptionDefinition>())
                .Select(option => new ReportableFieldOptionMetadata(option.Id, option.Label, option.Value))
                .ToArray(),
            Filterable: true,
            Sortable: true,
            Searchable: IsSearchable(field.Type),
            SupportsAggregation: field.Type == FormFieldTypes.Number,
            SupportsChoiceGrouping: isChoice);
    }

    private static bool IsSearchable(string type)
    {
        return type is FormFieldTypes.Text or FormFieldTypes.Textarea or FormFieldTypes.Email or FormFieldTypes.Phone or FormFieldTypes.Select or FormFieldTypes.Radio;
    }
}
```

- [ ] **Step 4: Run backend harness and verify it passes this task**

Run:

```bash
dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj
```

Expected: exit code 0.

---

### Task 2: Backend Report Config Validation Uses Metadata

**Files:**
- Modify: `src/api/Modules/Reports/ReportManagementContracts.cs`
- Modify: `src/api/Modules/Reports/ListReportConfigValidator.cs`
- Modify: `src/api.Tests/Program.cs`

- [ ] **Step 1: Add failing backend validation assertion**

Add near existing list report config assertions:

```csharp
AssertTrue(
    ListReportConfigValidator.Validate(
        reportingSchema,
        listReportConfig with
        {
            Columns = new[] { new ListReportColumnDefinition(ReportableSystemFields.UpdatedAt, "Updated date", true, 140) },
            Filters = new[] { new ListReportFilterDefinition(ReportableSystemFields.DepartmentId, ReportFilterOperators.Equal, sampleDepartmentId.ToString()) },
            Sort = new[] { new ListReportSortDefinition(ReportableSystemFields.OwnerId, ReportSortDirections.Asc) }
        }).Valid,
    "List report configs should validate against normalized system field metadata.");
```

- [ ] **Step 2: Run backend harness and verify it fails**

Run:

```bash
dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj
```

Expected: assertion failure because the existing validator only knows three system fields.

- [ ] **Step 3: Delegate report system constants to form metadata**

Modify `ReportSystemFields` in `src/api/Modules/Reports/ReportManagementContracts.cs`:

```csharp
public static class ReportSystemFields
{
    public const string Status = ReportableSystemFields.Status;
    public const string CreatedAt = ReportableSystemFields.CreatedAt;
    public const string CreatedById = ReportableSystemFields.CreatedById;
    public const string UpdatedAt = ReportableSystemFields.UpdatedAt;
    public const string UpdatedById = ReportableSystemFields.UpdatedById;
    public const string OwnerId = ReportableSystemFields.OwnerId;
    public const string DepartmentId = ReportableSystemFields.DepartmentId;

    public static IReadOnlySet<string> Supported { get; } = FormReportableFieldMetadata.SystemFields
        .Select(field => field.Id)
        .ToHashSet(StringComparer.Ordinal);
}
```

- [ ] **Step 4: Update validator field lookup**

In `ListReportConfigValidator.Validate`, replace the raw field set with:

```csharp
var validFields = FormReportableFieldMetadata.GetReportableFieldsById(schema);
```

Update helper signatures to accept `IReadOnlyDictionary<string, ReportableFieldMetadata>` and replace `validFields.Contains(fieldId)` with `validFields.ContainsKey(fieldId)`.

- [ ] **Step 5: Run backend harness and build**

Run:

```bash
dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj
dotnet build src/api/OpenBusinessPlatform.Api.csproj
```

Expected: both exit code 0.

---

### Task 3: Frontend Reportable Field Metadata

**Files:**
- Create: `src/app/src/features/forms/reportableFields.ts`
- Create: `src/app/src/features/forms/reportableFields.test.mjs`

- [ ] **Step 1: Add failing frontend metadata test**

Create `src/app/src/features/forms/reportableFields.test.mjs`:

```javascript
import assert from "node:assert/strict";
import { test } from "vitest";
import {
  getReportableFields,
  reportableSystemFields,
  reportableSystemFieldIds
} from "./reportableFields.ts";

test("reportable fields include normalized form metadata and system fields", () => {
  const schema = {
    schemaVersion: 1,
    fields: [
      { id: "employee_name", type: "text", label: "Employee name" },
      { id: "salary", type: "number", label: "Salary" },
      {
        id: "department",
        type: "select",
        label: "Department",
        options: [
          { id: "opt_hr", label: "Human Resources", value: "hr" },
          { id: "opt_finance", label: "Finance", value: "finance" }
        ]
      }
    ],
    layout: { pages: [] }
  };

  const fields = getReportableFields(schema);

  assert.equal(fields.find((field) => field.id === "employee_name").source, "form");
  assert.equal(fields.find((field) => field.id === "salary").supportsAggregation, true);
  assert.equal(fields.find((field) => field.id === "department").supportsChoiceGrouping, true);
  assert.equal(fields.find((field) => field.id === "department").options[0].label, "Human Resources");
  assert.equal(fields.some((field) => field.id === "updated_at" && field.source === "system"), true);
  assert.equal(fields.some((field) => field.id === "owner_id" && field.source === "system"), true);
  assert.equal(fields.some((field) => field.id === "department_id" && field.source === "system"), true);
  assert.equal(reportableSystemFields.length, reportableSystemFieldIds.length);
});
```

- [ ] **Step 2: Run frontend test and verify it fails**

Run:

```bash
cd src/app && npm test -- src/features/forms/reportableFields.test.mjs
```

Expected: module import failure because `reportableFields.ts` does not exist.

- [ ] **Step 3: Create frontend metadata helper**

Create `src/app/src/features/forms/reportableFields.ts` with exported system fields, IDs, metadata types, and `getReportableFields(schema)`.

- [ ] **Step 4: Run frontend metadata test**

Run:

```bash
cd src/app && npm test -- src/features/forms/reportableFields.test.mjs
```

Expected: 1 file passed.

---

### Task 4: Frontend Report Builder Uses Metadata

**Files:**
- Modify: `src/app/src/features/reports/types.ts`
- Modify: `src/app/src/features/reports/builder.ts`
- Modify: `src/app/src/features/reports/reportsApi.test.mjs`

- [ ] **Step 1: Add report builder assertions**

Add these imports to `src/app/src/features/reports/reportsApi.test.mjs`:

```javascript
import { createListReportConfig, getReportFieldOptions } from "./builder.ts";
```

Add this test block to `src/app/src/features/reports/reportsApi.test.mjs`:

```javascript
test("report builder field options use shared reportable metadata", () => {
  const schema = {
    schemaVersion: 1,
    fields: [
      {
        id: "department",
        type: "select",
        label: "Department",
        options: [{ id: "opt_hr", label: "Human Resources", value: "hr" }]
      }
    ],
    layout: { pages: [] }
  };

  const fields = getReportFieldOptions(schema);
  assert.equal(fields.some((field) => field.id === "updated_at" && field.source === "system"), true);
  assert.equal(fields.find((field) => field.id === "department").type, "select");
  assert.equal(fields.find((field) => field.id === "department").options[0].label, "Human Resources");
  assert.equal(createListReportConfig({ fieldOptions: fields, selectedFieldIds: ["department", "updated_at"] }).columns[1].width, 140);
});
```

- [ ] **Step 2: Run test and verify it fails before updating builder**

Run:

```bash
cd src/app && npm test -- src/features/reports/reportsApi.test.mjs
```

Expected: failure because the current report field options do not expose type/options and the report system field list does not include `updated_at`.

- [ ] **Step 3: Re-export system fields from forms metadata**

In `src/app/src/features/reports/types.ts`, replace the literal `reportSystemFields` array with:

```ts
import { reportableSystemFields } from "../forms/reportableFields";

export const reportSystemFields = reportableSystemFields;
```

- [ ] **Step 4: Update report builder helper**

In `src/app/src/features/reports/builder.ts`, import `getReportableFields` and map those fields to `ReportFieldOption` while preserving `id`, `label`, and `source`.

- [ ] **Step 5: Run frontend report tests**

Run:

```bash
cd src/app && npm test -- src/features/reports/reportsApi.test.mjs src/features/forms/reportableFields.test.mjs
```

Expected: both test files pass.

---

### Task 5: Documentation And Full Verification

**Files:**
- Modify: `tasks/v2/005-form-data-readiness.md`

- [ ] **Step 1: Mark task complete**

Update acceptance criteria to checked and add:

```markdown
## Current Status

Completed for the current V2 slice. Frontend and backend now expose shared reportable field metadata helpers for form fields, choice options, and normalized system fields. Existing report definition building and backend report config validation consume that metadata. This prepares report execution, dashboard summaries, chart widgets, validation rules, triggers, workflows, scheduled triggers, and future actions without changing persistence or published form versions.
```

- [ ] **Step 2: Run full verification**

Run:

```bash
cd src/app && npm test
cd src/app && npm run build
dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj
dotnet build src/api/OpenBusinessPlatform.Api.csproj
git diff --check
```

Expected: all commands exit 0.

- [ ] **Step 3: Commit implementation**

Run:

```bash
git add src/api/Modules/Forms/FormReportableFieldMetadata.cs src/api/Modules/Reports/ListReportConfigValidator.cs src/api/Modules/Reports/ReportManagementContracts.cs src/api.Tests/Program.cs src/app/src/features/forms/reportableFields.ts src/app/src/features/forms/reportableFields.test.mjs src/app/src/features/reports/builder.ts src/app/src/features/reports/types.ts src/app/src/features/reports/reportsApi.test.mjs tasks/v2/005-form-data-readiness.md
git commit -m "feat: add form data readiness metadata"
```
