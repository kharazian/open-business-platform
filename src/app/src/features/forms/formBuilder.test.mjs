import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { existsSync, rmSync } from "node:fs";
import { createRequire } from "node:module";

const outDir = "/tmp/obp-form-builder-test";

rmSync(outDir, { recursive: true, force: true });
execFileSync(
  "npx",
  [
    "tsc",
    "src/features/forms/types.ts",
    "src/features/forms/builder.ts",
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

const emittedBuilderPath = existsSync(`${outDir}/features/forms/builder.js`) ? `${outDir}/features/forms/builder.js` : `${outDir}/builder.js`;
const require = createRequire(import.meta.url);
const {
  addFieldToSchema,
  createEmptyFormBuilderSchema,
  createFormBuilderDraftStorageKey,
  deleteFieldFromSchema,
  fieldTypeLabels,
  getFieldLayoutWidth,
  layoutWidthOptions,
  loadFormBuilderDraft,
  saveFormBuilderDraft,
  updateFieldLayoutWidth,
  updateFieldInSchema
} = require(emittedBuilderPath);

const emptySchema = createEmptyFormBuilderSchema();

assert.equal(emptySchema.schemaVersion, 1);
assert.deepEqual(emptySchema.fields, []);
assert.equal(emptySchema.layout.pages[0].sections[0].rows.length, 0);
assert.equal(fieldTypeLabels.text, "Text");
assert.equal(fieldTypeLabels.radio, "Radio");

const textResult = addFieldToSchema(emptySchema, "text");
assert.equal(textResult.field.id, "text");
assert.equal(textResult.field.label, "Text");
assert.equal(textResult.schema.fields.length, 1);
assert.equal(textResult.schema.layout.pages[0].sections[0].rows[0].columns[0].span.desktop, 12);
assert.deepEqual(textResult.schema.layout.pages[0].sections[0].rows[0].columns[0].fields, ["text"]);
assert.equal(getFieldLayoutWidth(textResult.schema, "text"), "full");
assert.deepEqual(layoutWidthOptions.map((option) => option.value), ["full", "half", "third", "twoThirds"]);

const secondTextResult = addFieldToSchema(textResult.schema, "text");
assert.equal(secondTextResult.field.id, "text_2");

const emailResult = addFieldToSchema(textResult.schema, "email");
const halfTextSchema = updateFieldLayoutWidth(emailResult.schema, "text", "half");
assert.deepEqual(halfTextSchema.layout.pages[0].sections[0].rows[0].columns[0].span, {
  mobile: 12,
  tablet: 6,
  desktop: 6
});
assert.equal(getFieldLayoutWidth(halfTextSchema, "text"), "half");
assert.equal(halfTextSchema.layout.pages[0].sections[0].rows.length, 2);

const twoColumnSchema = updateFieldLayoutWidth(halfTextSchema, "email", "half");
assert.equal(twoColumnSchema.layout.pages[0].sections[0].rows.length, 1);
assert.deepEqual(
  twoColumnSchema.layout.pages[0].sections[0].rows[0].columns.map((column) => column.fields),
  [["text"], ["email"]]
);
assert.equal(getFieldLayoutWidth(twoColumnSchema, "email"), "half");

const twoThirdsSchema = updateFieldLayoutWidth(twoColumnSchema, "text", "twoThirds");
const thirdSchema = updateFieldLayoutWidth(twoThirdsSchema, "email", "third");
assert.deepEqual(
  thirdSchema.layout.pages[0].sections[0].rows[0].columns.map((column) => column.span.desktop),
  [8, 4]
);
assert.equal(getFieldLayoutWidth(thirdSchema, "text"), "twoThirds");
assert.equal(getFieldLayoutWidth(thirdSchema, "email"), "third");

assert.deepEqual(updateFieldLayoutWidth(thirdSchema, "missing", "half"), thirdSchema);

const selectResult = addFieldToSchema(emptySchema, "select");
assert.equal(selectResult.field.options.length, 2);
assert.equal(selectResult.field.options[0].label, "Option 1");
assert.equal(selectResult.field.options[0].value, "option_1");

const updatedTextSchema = updateFieldInSchema(textResult.schema, {
  ...textResult.field,
  label: "  Employee name  ",
  placeholder: "  Jane Cooper  ",
  helpText: "  Legal name  ",
  required: true
});
assert.equal(updatedTextSchema.fields[0].label, "Employee name");
assert.equal(updatedTextSchema.fields[0].placeholder, "Jane Cooper");
assert.equal(updatedTextSchema.fields[0].helpText, "Legal name");
assert.equal(updatedTextSchema.fields[0].required, true);

const updatedSelectSchema = updateFieldInSchema(selectResult.schema, {
  ...selectResult.field,
  options: [{ id: "", label: " High priority ", value: "" }]
});
assert.equal(updatedSelectSchema.fields[0].options[0].id, "select_option_1");
assert.equal(updatedSelectSchema.fields[0].options[0].label, "High priority");
assert.equal(updatedSelectSchema.fields[0].options[0].value, "high_priority");

const deletedTextSchema = deleteFieldFromSchema(updatedTextSchema, "text");
assert.equal(deletedTextSchema.fields.length, 0);
assert.equal(deletedTextSchema.layout.pages[0].sections[0].rows.length, 0);

const storageValues = new Map();
const storage = {
  getItem: (key) => storageValues.get(key) ?? null,
  setItem: (key, value) => storageValues.set(key, value)
};

saveFormBuilderDraft("form-1", updatedTextSchema, storage);
assert.deepEqual(loadFormBuilderDraft("form-1", storage), updatedTextSchema);

storage.setItem(createFormBuilderDraftStorageKey("form-broken"), "{not json");
assert.deepEqual(loadFormBuilderDraft("form-broken", storage), createEmptyFormBuilderSchema());
