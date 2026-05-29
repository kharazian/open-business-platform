import assert from "node:assert/strict";
import { test } from "vitest";
import {
  getReportableFields,
  reportableSystemFieldIds,
  reportableSystemFields
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
