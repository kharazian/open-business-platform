import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { test } from "vitest";
import {
  coerceFieldInputValue,
  createInitialRecordValues,
  getColumnSpanClass,
  getFieldErrorsById,
  getLayoutFields,
  getRenderableRows
} from "./renderer.ts";

test("form renderer helpers initialize, coerce, map errors, and build span classes", () => {
  const schema = {
    schemaVersion: 1,
    fields: [
      { id: "name", type: "text", label: "Name", defaultValue: "Ada" },
      { id: "amount", type: "number", label: "Amount", defaultValue: 25 },
      { id: "approved", type: "checkbox", label: "Approved", defaultValue: true },
      {
        id: "priority",
        type: "radio",
        label: "Priority",
        options: [
          { id: "priority_low", label: "Low", value: "low" },
          { id: "priority_high", label: "High", value: "high" }
        ],
        defaultValue: "high"
      },
      {
        id: "line_items",
        type: "subTable",
        label: "Line items",
        defaultValue: "ignored",
        subTable: {
          sourceType: "child_form_records",
          childFormId: "11111111-1111-1111-1111-111111111111",
          parentLookupFieldId: "parent_request",
          displayColumnFieldIds: ["item_name"]
        }
      },
      { id: "request_number", type: "autonumber", label: "Request number", defaultValue: "ignored", autonumber: { startAt: 1, padding: 0 } }
    ],
    layout: {
      pages: [
        {
          id: "page_1",
          sections: [
            {
              id: "section_1",
              rows: [
                {
                  id: "row_1",
                  columns: [{ id: "column_1", span: { mobile: 12, tablet: 6, desktop: 4 }, fields: ["name", "missing", "amount"] }]
                }
              ]
            }
          ]
        }
      ]
    }
  };

  assert.deepEqual(createInitialRecordValues(schema), {
    name: "Ada",
    amount: 25,
    approved: true,
    priority: "high",
    line_items: null
  });

  assert.equal(coerceFieldInputValue(schema.fields[1], "42"), 42);
  assert.equal(coerceFieldInputValue(schema.fields[1], ""), null);
  assert.equal(coerceFieldInputValue(schema.fields[2], false), false);
  assert.equal(coerceFieldInputValue(schema.fields[0], "Grace"), "Grace");
  assert.equal(coerceFieldInputValue({ id: "budget", type: "currency", label: "Budget" }, "42.75"), 42.75);
  assert.equal(coerceFieldInputValue({ id: "completion", type: "percent", label: "Completion" }, "87.5"), 87.5);
  assert.equal(coerceFieldInputValue({ id: "rating", type: "rating", label: "Rating" }, "4"), 4);
  assert.equal(coerceFieldInputValue(schema.fields[4], "child-row-value"), null);

  assert.deepEqual(
    getFieldErrorsById([
      { path: "values.name", code: "record.required", message: "Name is required." },
      { path: "values.amount", code: "record.type", message: "Amount must be numeric." },
      { path: "layout", code: "layout.field_missing", message: "Layout issue." }
    ]),
    {
      name: ["Name is required."],
      amount: ["Amount must be numeric."]
    }
  );

  assert.deepEqual(
    getLayoutFields(schema.layout.pages[0].sections[0].rows[0].columns[0], new Map(schema.fields.map((field) => [field.id, field]))).map(
      (field) => field.id
    ),
    ["name", "amount"]
  );

  assert.deepEqual(
    getRenderableRows({
      id: "section_with_empty_cells",
      rows: [
        { id: "empty_row", columns: [{ id: "empty_col", span: { mobile: 12, tablet: 12, desktop: 12 }, fields: [] }] },
        {
          id: "mixed_row",
          columns: [
            { id: "empty_side", span: { mobile: 12, tablet: 6, desktop: 6 }, fields: [] },
            { id: "filled_side", span: { mobile: 12, tablet: 6, desktop: 6 }, fields: ["name"] }
          ]
        }
      ]
    }).map((row) => ({
      id: row.id,
      columns: row.columns.map((column) => column.id)
    })),
    [{ id: "mixed_row", columns: ["filled_side"] }]
  );

  assert.equal(getColumnSpanClass(schema.layout.pages[0].sections[0].rows[0].columns[0], "mobile"), "col-span-12");
  assert.equal(getColumnSpanClass(schema.layout.pages[0].sections[0].rows[0].columns[0], "tablet"), "col-span-6");
  assert.equal(getColumnSpanClass(schema.layout.pages[0].sections[0].rows[0].columns[0], "desktop"), "col-span-4");
  assert.equal(getColumnSpanClass(schema.layout.pages[0].sections[0].rows[0].columns[0], "responsive"), "col-span-12 md:col-span-6 xl:col-span-4");
});

test("form renderer keeps autonumbers server-generated and read-only", () => {
  const rendererSource = readFileSync(new URL("./components/FormRenderer.tsx", import.meta.url), "utf8");

  assert.deepEqual(createInitialRecordValues({ schemaVersion: 1, fields: [{ id: "request_number", type: "autonumber", label: "Request number", autonumber: { startAt: 1, padding: 0 } }], layout: { pages: [] } }), {});
  assert.equal(rendererSource.includes('field.type === "autonumber"'), true);
  assert.equal(rendererSource.includes('help={field.helpText ?? "Generated when the record is created."}'), true);
});

test("form renderer uses protected attachment upload and download paths", () => {
  const rendererSource = readFileSync(new URL("./components/FormRenderer.tsx", import.meta.url), "utf8");
  assert.equal(rendererSource.includes("FileAttachmentField"), true);
  assert.equal(rendererSource.includes("uploadFileAttachment"), true);
  assert.equal(rendererSource.includes("deletePendingFileAttachment"), true);
  assert.equal(rendererSource.includes("getFileAttachmentDownloadUrl"), true);
  assert.equal(rendererSource.includes('type="file"'), true);
});

test("form renderer exposes read-only sub-table preview", () => {
  const rendererSource = readFileSync(new URL("./components/FormRenderer.tsx", import.meta.url), "utf8");
  const recordDetailSource = readFileSync(new URL("../records/pages/RecordDetailPage.tsx", import.meta.url), "utf8");

  assert.equal(rendererSource.includes("SubTablePreviewField"), true);
  assert.equal(rendererSource.includes('field.type === "subTable"'), true);
  assert.equal(rendererSource.includes("Related child records"), true);
  assert.equal(rendererSource.includes("displayColumnFieldIds"), true);
  assert.equal(rendererSource.includes("listSubTableRows"), true);
  assert.equal(rendererSource.includes("recordId?: string"), true);
  assert.equal(rendererSource.includes("Loading child records"), true);
  assert.equal(rendererSource.includes("No child records found."), true);
  assert.equal(rendererSource.includes("openAddRowModal"), true);
  assert.equal(rendererSource.includes("submitRecord(childForm.id"), true);
  assert.equal(rendererSource.includes("[field.subTable.parentLookupFieldId]: recordId"), true);
  assert.equal(rendererSource.includes("sortFieldId"), true);
  assert.equal(rendererSource.includes("sortDirection"), true);
  assert.equal(rendererSource.includes("columnFilters"), true);
  assert.equal(rendererSource.includes("Page"), true);
  assert.equal(rendererSource.includes("canEdit"), true);
  assert.equal(rendererSource.includes("canDelete"), true);
  assert.equal(rendererSource.includes("openEditRowModal"), true);
  assert.equal(rendererSource.includes("saveEditedChildRow"), true);
  assert.equal(rendererSource.includes("deleteChildRow"), true);
  assert.equal(rendererSource.includes("getRecord(recordIdToEdit)"), true);
  assert.equal(rendererSource.includes("updateRecord(editingChildRecord.id"), true);
  assert.equal(rendererSource.includes("deleteRecord(recordIdToDelete)"), true);
  assert.equal(rendererSource.includes("Edit row"), true);
  assert.equal(rendererSource.includes("Delete row"), true);
  assert.equal(recordDetailSource.includes("SubTablePreviewField"), true);
  assert.equal(recordDetailSource.includes("recordId={record.id}"), true);
});

test("form renderer wires record lookup fields to lookup options API", () => {
  const rendererSource = readFileSync(new URL("./components/FormRenderer.tsx", import.meta.url), "utf8");
  const submitPageSource = readFileSync(new URL("./pages/SubmitFormPage.tsx", import.meta.url), "utf8");
  const recordDetailSource = readFileSync(new URL("../records/pages/RecordDetailPage.tsx", import.meta.url), "utf8");

  assert.equal(rendererSource.includes("listLookupOptions"), true);
  assert.equal(rendererSource.includes("RecordLookupField"), true);
  assert.equal(rendererSource.includes('field.type === "recordLookup"'), true);
  assert.equal(rendererSource.includes("formId?: string"), true);
  assert.equal(rendererSource.includes("lookupDisplayValues?: Record<string, string>"), true);
  assert.equal(rendererSource.includes("displayValue={lookupDisplayValues?.[field.id]}"), true);
  assert.equal(rendererSource.includes("dependencies={values}"), true);
  assert.equal(rendererSource.includes('aria-label="Search lookup records"'), true);
  assert.equal(submitPageSource.includes("formId={form.id}"), true);
  assert.equal(recordDetailSource.includes("formId={record.formId}"), true);
  assert.equal(recordDetailSource.includes("lookupDisplayValues={record.displayValues}"), true);
});

test("form renderer wires user and department pickers to directory options", () => {
  const rendererSource = readFileSync(new URL("./components/FormRenderer.tsx", import.meta.url), "utf8");
  const directoryApiSource = readFileSync(new URL("./directoryApi.ts", import.meta.url), "utf8");

  assert.equal(rendererSource.includes("DirectoryPickerField"), true);
  assert.equal(rendererSource.includes('field.type === "userPicker"'), true);
  assert.equal(rendererSource.includes('field.type === "departmentPicker"'), true);
  assert.equal(rendererSource.includes("listDirectoryUsers"), true);
  assert.equal(rendererSource.includes("listDirectoryDepartments"), true);
  assert.equal(rendererSource.includes('"Select a user"'), true);
  assert.equal(rendererSource.includes('"Select a department"'), true);
  assert.equal(directoryApiSource.includes('"/api/directory/users"'), true);
  assert.equal(directoryApiSource.includes('"/api/directory/departments"'), true);
});
