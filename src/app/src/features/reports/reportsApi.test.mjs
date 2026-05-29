import assert from "node:assert/strict";
import { test } from "vitest";
import * as api from "./api.ts";
import { createListReportConfig, getReportFieldOptions } from "./builder.ts";

test("reports API client maps list report requests and errors", async () => {
  const calls = [];
  const config = {
    schemaVersion: 1,
    columns: [{ fieldId: "site_name", label: "Site name", visible: true, width: 180 }],
    filters: [{ fieldId: "status", operator: "equals", value: "active" }],
    sort: [{ fieldId: "created_at", direction: "desc" }]
  };
  const fetcher = async (input, init = {}) => {
    calls.push({ input, init });

    if (input === "/api/forms/form-2/reports" && init.method === "GET") {
      return {
        ok: true,
        json: async () => ({
          items: [
            {
              id: "report-1",
              formId: "form-2",
              formName: "Safety inspection",
              name: "Open inspections",
              type: "list",
              columnCount: 1,
              filterCount: 1,
              sortCount: 1,
              concurrencyStamp: "report-stamp",
              createdAt: "2026-05-22T12:00:00.000Z",
              createdById: null,
              updatedAt: null,
              updatedById: null
            }
          ]
        })
      };
    }

    if (input === "/api/forms/form-2/reports" && init.method === "POST") {
      const body = JSON.parse(init.body);

      return {
        ok: true,
        json: async () => ({
          id: "report-2",
          formId: "form-2",
          formName: "Safety inspection",
          name: body.name,
          type: "list",
          config: body.config,
          concurrencyStamp: "report-stamp-2",
          createdAt: "2026-05-22T12:05:00.000Z",
          createdById: null,
          updatedAt: null,
          updatedById: null
        })
      };
    }

    return { ok: false, json: async () => ({ message: "Unexpected request." }) };
  };

  const reports = await api.listReports("form-2", fetcher);
  const created = await api.createListReport("form-2", { name: "Open inspections", config }, fetcher);

  assert.equal(reports[0].name, "Open inspections");
  assert.equal(reports[0].columnCount, 1);
  assert.equal(created.name, "Open inspections");
  assert.equal(created.config.columns[0].fieldId, "site_name");
  assert.equal(calls[0].input, "/api/forms/form-2/reports");
  assert.equal(calls[0].init.method, "GET");
  assert.equal(calls[0].init.credentials, "include");
  assert.equal(calls[1].input, "/api/forms/form-2/reports");
  assert.equal(calls[1].init.method, "POST");
  assert.equal(calls[1].init.credentials, "include");
  assert.equal(calls[1].init.headers["Content-Type"], "application/json");
  assert.equal(JSON.parse(calls[1].init.body).name, "Open inspections");
  assert.deepEqual(JSON.parse(calls[1].init.body).config, config);

  await assert.rejects(
    () => api.listReports("form-2", async () => ({ ok: true, json: async () => ({}) })),
    /API response did not include an items collection/
  );

  await assert.rejects(
    () =>
      api.createListReport(
        "form-2",
        { name: "", config },
        async () => ({
          ok: false,
          json: async () => ({
            message: "Report config is invalid.",
            errors: [{ path: "config.columns", code: "report.columns.required", message: "Choose at least one visible column." }]
          })
        })
      ),
    (error) => {
      assert.equal(error.name, "ReportsApiError");
      assert.equal(error.message, "Report config is invalid.");
      assert.equal(error.errors[0].path, "config.columns");
      return true;
    }
  );
});

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
