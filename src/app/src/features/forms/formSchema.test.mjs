import assert from "node:assert/strict";
import { test } from "vitest";
import { validateFormSchema, validateRecordValues } from "./validation.ts";

test("form schema and record validation catches invalid definitions and values", () => {
  const validSchema = {
    schemaVersion: 1,
    fields: [
      { id: "first_name", type: "text", label: "First name", required: true },
      { id: "email", type: "email", label: "Email", required: true },
      {
        id: "department",
        type: "select",
        label: "Department",
        options: [
          { id: "opt_finance", label: "Finance", value: "finance" },
          { id: "opt_ops", label: "Operations", value: "operations" }
        ]
      },
      { id: "active", type: "checkbox", label: "Active employee" }
    ],
    layout: {
      pages: [
        {
          id: "page_1",
          title: "Employee",
          sections: [
            {
              id: "section_1",
              title: "Basic info",
              rows: [
                {
                  id: "row_1",
                  columns: [
                    {
                      id: "col_1",
                      span: { mobile: 12, tablet: 6, desktop: 6 },
                      fields: ["first_name", "email"]
                    },
                    {
                      id: "col_2",
                      span: { mobile: 12, tablet: 6, desktop: 6 },
                      fields: ["department", "active"]
                    }
                  ]
                }
              ]
            }
          ]
        }
      ]
    }
  };

  assert.deepEqual(validateFormSchema(validSchema), { valid: true, errors: [] });

  assert.equal(
    validateFormSchema({
      ...validSchema,
      fields: [
        validSchema.fields[0],
        { ...validSchema.fields[0], label: "Duplicate first name" }
      ]
    }).errors.some((error) => error.code === "field.duplicate_id"),
    true
  );

  assert.equal(
    validateFormSchema({
      ...validSchema,
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
                    columns: [{ id: "col_1", span: { mobile: 12, tablet: 12, desktop: 13 }, fields: ["missing_field"] }]
                  }
                ]
              }
            ]
          }
        ]
      }
    }).errors.some((error) => error.code === "layout.field_unknown"),
    true
  );

  assert.equal(
    validateFormSchema({
      ...validSchema,
      fields: [{ id: "department", type: "select", label: "Department", options: [] }],
      layout: validSchema.layout
    }).errors.some((error) => error.code === "field.options_required"),
    true
  );

  assert.deepEqual(validateRecordValues(validSchema, {
    first_name: "Ada",
    email: "ada@example.com",
    department: "finance",
    active: true
  }), { valid: true, errors: [] });

  const invalidRecord = validateRecordValues(validSchema, {
    first_name: "",
    email: "not-an-email",
    department: "legal",
    active: "true",
    unexpected: "value"
  });

  assert.equal(invalidRecord.errors.some((error) => error.code === "record.required"), true);
  assert.equal(invalidRecord.errors.some((error) => error.code === "record.email"), true);
  assert.equal(invalidRecord.errors.some((error) => error.code === "record.option_unknown"), true);
  assert.equal(invalidRecord.errors.some((error) => error.code === "record.type"), true);
  assert.equal(invalidRecord.errors.some((error) => error.code === "record.field_unknown"), true);
});

test("form schema supports record lookup fields with lookup config", () => {
  const lookupSchema = {
    schemaVersion: 1,
    fields: [
      {
        id: "customer",
        type: "recordLookup",
        label: "Customer",
        required: true,
        lookup: {
          sourceType: "form_records",
          sourceFormId: "11111111-1111-1111-1111-111111111111",
          labelFieldIds: ["customer_name"],
          searchFieldIds: ["customer_name", "customer_code"]
        }
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
                  columns: [
                    {
                      id: "col_1",
                      span: { mobile: 12, tablet: 12, desktop: 12 },
                      fields: ["customer"]
                    }
                  ]
                }
              ]
            }
          ]
        }
      ]
    }
  };

  assert.deepEqual(validateFormSchema(lookupSchema), { valid: true, errors: [] });
  assert.deepEqual(validateRecordValues(lookupSchema, {
    customer: "22222222-2222-2222-2222-222222222222"
  }), { valid: true, errors: [] });
  assert.equal(validateRecordValues(lookupSchema, { customer: 123 }).errors.some((error) => error.code === "record.lookup_type"), true);
  assert.equal(
    validateFormSchema({
      ...lookupSchema,
      fields: [{ id: "customer", type: "recordLookup", label: "Customer" }]
    }).errors.some((error) => error.code === "field.lookup_required"),
    true
  );
});
