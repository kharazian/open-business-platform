import assert from "node:assert/strict";
import { test } from "vitest";
import {
  createDefaultPrintTemplateConfig,
  createPrintTemplateDraft,
  validatePrintTemplateDraft
} from "./templateBuilder.ts";
import { buildRecordTemplateRows, buildReportTemplateRows, getPrintTemplatePdfButtonLabel } from "./templateRenderer.ts";

const schema = {
  schemaVersion: 1,
  fields: [
    { id: "first_name", type: "text", label: "First name" },
    { id: "department", type: "select", label: "Department" }
  ],
  layout: { pages: [] }
};

test("print template builder creates valid record template defaults", () => {
  const draft = createPrintTemplateDraft("record", "Employee onboarding");

  assert.equal(draft.name, "Employee onboarding record template");
  assert.equal(draft.type, "record");
  assert.equal(draft.config.header.title, "Employee onboarding");
  assert.equal(draft.config.sections[0].kind, "fields");
  assert.deepEqual(validatePrintTemplateDraft(draft).errors, []);
});

test("print template validation rejects missing name and wrong report scope", () => {
  const draft = {
    ...createPrintTemplateDraft("report", "Employee onboarding"),
    name: " ",
    reportId: null
  };

  const validation = validatePrintTemplateDraft(draft);

  assert.equal(validation.valid, false);
  assert.deepEqual(
    validation.errors.map((error) => error.code),
    ["print_template.name.required", "print_template.report.required"]
  );
});

test("record template renderer respects selected fields and signatures", () => {
  const config = {
    ...createDefaultPrintTemplateConfig("record", "Employee onboarding"),
    sections: [
      { id: "main", kind: "fields", title: "Visible fields", fieldIds: ["department", "missing"] },
      { id: "sign", kind: "signature", title: "Approval", signatureLabels: ["Manager", "Employee"] }
    ]
  };

  const rows = buildRecordTemplateRows(config, schema, {
    first_name: "Ava",
    department: "Finance"
  });

  assert.deepEqual(rows.fields.map((row) => [row.label, row.displayValue]), [["Department", "Finance"]]);
  assert.deepEqual(rows.signatures, ["Manager", "Employee"]);
});

test("report template renderer returns selected columns and pdf action label", () => {
  const config = {
    ...createDefaultPrintTemplateConfig("report", "Employee report"),
    sections: [{ id: "table", kind: "table", title: "Rows", fieldIds: ["department"] }]
  };
  const execution = {
    columns: [
      { fieldId: "first_name", label: "First name" },
      { fieldId: "department", label: "Department" }
    ],
    rows: [
      { id: "1", cells: { department: { displayValue: "Finance" } } }
    ]
  };

  const table = buildReportTemplateRows(config, execution);

  assert.deepEqual(table.columns.map((column) => column.label), ["Department"]);
  assert.equal(table.rows[0].cells.department.displayValue, "Finance");
  assert.equal(getPrintTemplatePdfButtonLabel(config), "Generate PDF");
});
