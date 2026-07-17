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
          searchFieldIds: ["customer_name", "customer_code"],
          filters: [
            {
              sourceFieldId: "department",
              valueFromFieldId: "request_department"
            }
          ]
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
      fields: [
        {
          ...lookupSchema.fields[0],
          lookup: {
            ...lookupSchema.fields[0].lookup,
            filters: [{ sourceFieldId: "", valueFromFieldId: "request_department" }]
          }
        }
      ]
    }).errors.some((error) => error.code === "field.lookup_filter_required"),
    true
  );
  assert.equal(
    validateFormSchema({
      ...lookupSchema,
      fields: [{ id: "customer", type: "recordLookup", label: "Customer" }]
    }).errors.some((error) => error.code === "field.lookup_required"),
    true
  );
});

test("form schema supports read-only sub-table fields with child form config", () => {
  const subTableSchema = {
    schemaVersion: 1,
    fields: [
      {
        id: "line_items",
        type: "subTable",
        label: "Line items",
        subTable: {
          sourceType: "child_form_records",
          childFormId: "11111111-1111-1111-1111-111111111111",
          parentLookupFieldId: "parent_request",
          displayColumnFieldIds: ["item_name", "quantity", "price"],
          allowInlineCreate: false,
          allowInlineEdit: false,
          allowInlineDelete: false,
          minRows: 0,
          maxRows: 25
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
                      fields: ["line_items"]
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

  assert.deepEqual(validateFormSchema(subTableSchema), { valid: true, errors: [] });
  assert.deepEqual(validateRecordValues(subTableSchema, {}), { valid: true, errors: [] });
  assert.equal(
    validateRecordValues(subTableSchema, { line_items: "embedded-child-data" }).errors.some((error) => error.code === "record.sub_table_readonly"),
    true
  );
  assert.equal(
    validateFormSchema({
      ...subTableSchema,
      fields: [{ id: "line_items", type: "subTable", label: "Line items" }]
    }).errors.some((error) => error.code === "field.sub_table_required"),
    true
  );
  assert.equal(
    validateFormSchema({
      ...subTableSchema,
      fields: [
        {
          ...subTableSchema.fields[0],
          subTable: {
            ...subTableSchema.fields[0].subTable,
            parentLookupFieldId: "",
            displayColumnFieldIds: [],
            minRows: 10,
            maxRows: 3
          }
        }
      ]
    }).errors.some((error) => error.code === "field.sub_table_display_fields_required"),
    true
  );
  assert.equal(
    validateFormSchema({
      ...subTableSchema,
      fields: [
        {
          ...subTableSchema.fields[0],
          subTable: {
            ...subTableSchema.fields[0].subTable,
            parentLookupFieldId: "",
            displayColumnFieldIds: [],
            minRows: 10,
            maxRows: 3
          }
        }
      ]
    }).errors.some((error) => error.code === "field.sub_table_row_range"),
    true
  );
});

test("form schema supports practical business field types", () => {
  const schema = {
    schemaVersion: 1,
    fields: [
      { id: "attachment", type: "fileUpload", label: "Attachment" },
      { id: "budget", type: "currency", label: "Budget" },
      { id: "completion", type: "percent", label: "Completion" },
      { id: "priority", type: "rating", label: "Priority" },
      { id: "website", type: "url", label: "Website" },
      { id: "start_time", type: "time", label: "Start time" },
      { id: "starts_at", type: "datetime", label: "Starts at" },
      { id: "owner", type: "userPicker", label: "Owner" },
      { id: "department", type: "departmentPicker", label: "Department" }
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
                      fields: [
                        "attachment",
                        "budget",
                        "completion",
                        "priority",
                        "website",
                        "start_time",
                        "starts_at",
                        "owner",
                        "department"
                      ]
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

  assert.deepEqual(validateFormSchema(schema), { valid: true, errors: [] });
  assert.deepEqual(validateRecordValues(schema, {
    attachment: "pending-upload.pdf",
    budget: 1250.5,
    completion: 87.25,
    priority: 4,
    website: "https://example.com/request",
    start_time: "09:30",
    starts_at: "2026-06-25T09:30",
    owner: "11111111-1111-1111-1111-111111111111",
    department: "22222222-2222-2222-2222-222222222222"
  }), { valid: true, errors: [] });

  const invalid = validateRecordValues(schema, {
    attachment: 123,
    budget: "1250",
    completion: 125,
    priority: 6,
    website: "not-a-url",
    start_time: "25:00",
    starts_at: "2026-06-25",
    owner: "not-a-user-id",
    department: "not-a-department-id"
  });

  assert.equal(invalid.errors.some((error) => error.path === "values.attachment" && error.code === "record.type"), true);
  assert.equal(invalid.errors.some((error) => error.path === "values.budget" && error.code === "record.type"), true);
  assert.equal(invalid.errors.some((error) => error.path === "values.completion" && error.code === "record.percent"), true);
  assert.equal(invalid.errors.some((error) => error.path === "values.priority" && error.code === "record.rating"), true);
  assert.equal(invalid.errors.some((error) => error.path === "values.website" && error.code === "record.url"), true);
  assert.equal(invalid.errors.some((error) => error.path === "values.start_time" && error.code === "record.time"), true);
  assert.equal(invalid.errors.some((error) => error.path === "values.starts_at" && error.code === "record.datetime"), true);
  assert.equal(invalid.errors.some((error) => error.path === "values.owner" && error.code === "record.user_picker_type"), true);
  assert.equal(invalid.errors.some((error) => error.path === "values.department" && error.code === "record.department_picker_type"), true);
});

test("file upload fields validate bounded storage configuration", () => {
  const field = { id: "attachment", type: "fileUpload", label: "Attachment", fileUpload: { maxSizeBytes: 1024, allowedContentTypes: ["application/pdf"] } };
  const schema = { schemaVersion: 1, fields: [field], layout: { pages: [{ id: "page_1", sections: [{ id: "section_1", rows: [{ id: "row_1", columns: [{ id: "col_1", span: { mobile: 12, tablet: 12, desktop: 12 }, fields: ["attachment"] }] }] }] }] } };
  assert.deepEqual(validateFormSchema(schema), { valid: true, errors: [] });
  const invalid = { ...schema, fields: [{ ...field, fileUpload: { maxSizeBytes: 11 * 1024 * 1024, allowedContentTypes: ["application/x-msdownload"] } }] };
  assert.equal(validateFormSchema(invalid).errors.some((error) => error.code === "field.file_upload_size"), true);
  assert.equal(validateFormSchema(invalid).errors.some((error) => error.code === "field.file_upload_type_unsupported"), true);
});

test("structured address fields validate bounded values and required parts", () => {
  const schema = {
    schemaVersion: 1,
    fields: [{ id: "site_address", type: "address", label: "Site address", address: { requiredSubfields: ["line1", "country"] } }],
    layout: { pages: [{ id: "page_1", sections: [{ id: "section_1", rows: [{ id: "row_1", columns: [{ id: "col_1", span: { mobile: 12, tablet: 12, desktop: 12 }, fields: ["site_address"] }] }] }] }] }
  };
  assert.deepEqual(validateFormSchema(schema), { valid: true, errors: [] });
  assert.deepEqual(validateRecordValues(schema, { site_address: { line1: "100 King Street West", city: "Toronto", country: "Canada", latitude: 43.648, longitude: -79.381 } }), { valid: true, errors: [] });
  const invalid = validateRecordValues(schema, { site_address: { country: "", latitude: 91, secret: "no" } });
  assert.equal(invalid.errors.some((error) => error.path === "values.site_address.line1" && error.code === "record.address_member_required"), true);
  assert.equal(invalid.errors.some((error) => error.code === "record.address_coordinate_range"), true);
  assert.equal(invalid.errors.some((error) => error.code === "record.address_member_unknown"), true);
  const invalidConfig = { ...schema, fields: [{ ...schema.fields[0], address: { requiredSubfields: ["unsupported"] } }] };
  assert.equal(validateFormSchema(invalidConfig).errors.some((error) => error.code === "field.address_subfield_unknown"), true);
});

test("autonumber fields validate bounded configuration and generated strings", () => {
  const schema = { schemaVersion: 1, fields: [{ id: "request_number", type: "autonumber", label: "Request number", autonumber: { prefix: "REQ-", suffix: "-CA", startAt: 42, padding: 6 } }], layout: { pages: [{ id: "page_1", sections: [{ id: "section_1", rows: [{ id: "row_1", columns: [{ id: "col_1", span: { mobile: 12, tablet: 12, desktop: 12 }, fields: ["request_number"] }] }] }] }] } };
  assert.deepEqual(validateFormSchema(schema), { valid: true, errors: [] });
  assert.deepEqual(validateRecordValues(schema, { request_number: "REQ-000042-CA" }), { valid: true, errors: [] });
  const invalid = { ...schema, fields: [{ ...schema.fields[0], autonumber: { prefix: "x".repeat(41), startAt: -1, padding: 19 } }] };
  assert.equal(validateFormSchema(invalid).errors.some((error) => error.code === "field.autonumber_start"), true);
  assert.equal(validateFormSchema(invalid).errors.some((error) => error.code === "field.autonumber_padding"), true);
  const unsafeStart = { ...schema, fields: [{ ...schema.fields[0], autonumber: { startAt: Number.MAX_SAFE_INTEGER, padding: 0 } }] };
  assert.equal(validateFormSchema(unsafeStart).errors.some((error) => error.code === "field.autonumber_start"), true);
});
