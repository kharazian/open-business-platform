import assert from "node:assert/strict";
import { test } from "vitest";
import {
  coerceFieldInputValue,
  createInitialRecordValues,
  getColumnSpanClass,
  getFieldErrorsById,
  getLayoutFields
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
      }
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
    priority: "high"
  });

  assert.equal(coerceFieldInputValue(schema.fields[1], "42"), 42);
  assert.equal(coerceFieldInputValue(schema.fields[1], ""), null);
  assert.equal(coerceFieldInputValue(schema.fields[2], false), false);
  assert.equal(coerceFieldInputValue(schema.fields[0], "Grace"), "Grace");

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

  assert.equal(getColumnSpanClass(schema.layout.pages[0].sections[0].rows[0].columns[0], "mobile"), "col-span-12");
  assert.equal(getColumnSpanClass(schema.layout.pages[0].sections[0].rows[0].columns[0], "tablet"), "col-span-6");
  assert.equal(getColumnSpanClass(schema.layout.pages[0].sections[0].rows[0].columns[0], "desktop"), "col-span-4");
  assert.equal(getColumnSpanClass(schema.layout.pages[0].sections[0].rows[0].columns[0], "responsive"), "col-span-12 md:col-span-6 xl:col-span-4");
});
