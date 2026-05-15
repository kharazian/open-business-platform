import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { existsSync, rmSync } from "node:fs";
import { createRequire } from "node:module";

const outDir = "/tmp/obp-form-schema-test";

rmSync(outDir, { recursive: true, force: true });
execFileSync(
  "npx",
  [
    "tsc",
    "src/features/forms/types.ts",
    "src/features/forms/validation.ts",
    "--ignoreConfig",
    "--target",
    "ES2022",
    "--module",
    "CommonJS",
    "--moduleResolution",
    "Node",
    "--ignoreDeprecations",
    "6.0",
    "--outDir",
    outDir,
    "--skipLibCheck",
    "--strict"
  ],
  { stdio: "inherit" }
);

const emittedValidationPath = existsSync(`${outDir}/features/forms/validation.js`)
  ? `${outDir}/features/forms/validation.js`
  : `${outDir}/validation.js`;
const require = createRequire(import.meta.url);
const { validateFormSchema, validateRecordValues } = require(emittedValidationPath);

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
