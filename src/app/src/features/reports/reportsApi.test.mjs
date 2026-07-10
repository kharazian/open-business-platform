import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
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

    if (input === "/api/forms/form-2/reports/report-1" && init.method === "GET") {
      return {
        ok: true,
        json: async () => ({
          id: "report-1",
          formId: "form-2",
          formName: "Safety inspection",
          name: "Open inspections",
          type: "list",
          config,
          concurrencyStamp: "report-stamp",
          createdAt: "2026-05-22T12:00:00.000Z",
          createdById: null,
          updatedAt: null,
          updatedById: null
        })
      };
    }

    if (input === "/api/forms/form-2/reports/report-1" && init.method === "PUT") {
      const body = JSON.parse(init.body);

      return {
        ok: true,
        json: async () => ({
          id: "report-1",
          formId: "form-2",
          formName: "Safety inspection",
          name: body.name,
          type: "list",
          config: body.config,
          concurrencyStamp: "report-stamp-updated",
          createdAt: "2026-05-22T12:00:00.000Z",
          createdById: null,
          updatedAt: "2026-05-22T12:15:00.000Z",
          updatedById: null
        })
      };
    }

    if (input === "/api/forms/form-2/reports/report-1" && init.method === "DELETE") {
      return {
        ok: true,
        json: async () => null
      };
    }

    if (input === "/api/forms/form-2/reports/report-1/run?page=2&pageSize=10&search=Jane&sortFieldId=site_name&sortDirection=asc&filter.site_name=Warehouse" && init.method === "GET") {
      return {
        ok: true,
        json: async () => ({
          reportId: "report-1",
          formId: "form-2",
          reportName: "Open inspections",
          formName: "Safety inspection",
          page: 2,
          pageSize: 10,
          totalCount: 12,
          columns: [{ fieldId: "site_name", label: "Site name", type: "text", source: "form", width: 180 }],
          rows: [
            {
              recordId: "record-1",
              status: "active",
              cells: {
                site_name: { value: "Warehouse A", displayValue: "Warehouse A" }
              },
              createdAt: "2026-05-22T12:10:00.000Z"
            }
          ]
        })
      };
    }

    return { ok: false, json: async () => ({ message: "Unexpected request." }) };
  };

  const reports = await api.listReports("form-2", fetcher);
  const created = await api.createListReport("form-2", { name: "Open inspections", config }, fetcher);
  const detail = await api.getListReport("form-2", "report-1", fetcher);
  const updated = await api.updateListReport("form-2", "report-1", { name: "Updated inspections", config, concurrencyStamp: detail.concurrencyStamp }, fetcher);
  await api.deleteListReport("form-2", "report-1", fetcher);
  const executed = await api.executeListReport(
    "form-2",
    "report-1",
    {
      page: 2,
      pageSize: 10,
      search: "Jane",
      sortFieldId: "site_name",
      sortDirection: "asc",
      filters: { site_name: "Warehouse" }
    },
    fetcher
  );
  const exportUrl = api.getListReportCsvExportUrl("form-2", "report-1", {
    search: " Jane ",
    sortFieldId: "site_name",
    sortDirection: "asc",
    filters: { site_name: "Warehouse" }
  });
  let downloadedUrl = "";
  api.downloadListReportCsv(
    "form-2",
    "report-1",
    {
      search: " Jane ",
      sortFieldId: "site_name",
      sortDirection: "asc",
      filters: { site_name: "Warehouse" }
    },
    (url) => {
      downloadedUrl = url;
    }
  );

  assert.equal(reports[0].name, "Open inspections");
  assert.equal(reports[0].columnCount, 1);
  assert.equal(created.name, "Open inspections");
  assert.equal(created.config.columns[0].fieldId, "site_name");
  assert.equal(detail.id, "report-1");
  assert.equal(detail.concurrencyStamp, "report-stamp");
  assert.equal(updated.name, "Updated inspections");
  assert.equal(updated.concurrencyStamp, "report-stamp-updated");
  assert.equal(executed.totalCount, 12);
  assert.equal(executed.columns[0].fieldId, "site_name");
  assert.equal(executed.rows[0].cells.site_name.displayValue, "Warehouse A");
  assert.equal(calls[0].input, "/api/forms/form-2/reports");
  assert.equal(calls[0].init.method, "GET");
  assert.equal(calls[0].init.credentials, "include");
  assert.equal(calls[1].input, "/api/forms/form-2/reports");
  assert.equal(calls[1].init.method, "POST");
  assert.equal(calls[1].init.credentials, "include");
  assert.equal(calls[1].init.headers["Content-Type"], "application/json");
  assert.equal(JSON.parse(calls[1].init.body).name, "Open inspections");
  assert.deepEqual(JSON.parse(calls[1].init.body).config, config);
  assert.equal(calls[2].input, "/api/forms/form-2/reports/report-1");
  assert.equal(calls[2].init.method, "GET");
  assert.equal(calls[2].init.credentials, "include");
  assert.equal(calls[3].input, "/api/forms/form-2/reports/report-1");
  assert.equal(calls[3].init.method, "PUT");
  assert.equal(calls[3].init.credentials, "include");
  assert.equal(calls[3].init.headers["Content-Type"], "application/json");
  assert.equal(JSON.parse(calls[3].init.body).name, "Updated inspections");
  assert.equal(JSON.parse(calls[3].init.body).concurrencyStamp, "report-stamp");
  assert.deepEqual(JSON.parse(calls[3].init.body).config, config);
  assert.equal(calls[4].input, "/api/forms/form-2/reports/report-1");
  assert.equal(calls[4].init.method, "DELETE");
  assert.equal(calls[4].init.credentials, "include");
  assert.equal(calls[5].input, "/api/forms/form-2/reports/report-1/run?page=2&pageSize=10&search=Jane&sortFieldId=site_name&sortDirection=asc&filter.site_name=Warehouse");
  assert.equal(calls[5].init.method, "GET");
  assert.equal(calls[5].init.credentials, "include");
  assert.equal(exportUrl, "/api/forms/form-2/reports/report-1/export.csv?search=Jane&sortFieldId=site_name&sortDirection=asc&filter.site_name=Warehouse");
  assert.equal(downloadedUrl, "/api/forms/form-2/reports/report-1/export.csv?search=Jane&sortFieldId=site_name&sortDirection=asc&filter.site_name=Warehouse");

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

test("reports page exposes runtime column filters and clickable sort headers", () => {
  const source = readFileSync(new URL("./pages/ReportsPage.tsx", import.meta.url), "utf8");
  const apiSource = readFileSync(new URL("./api.ts", import.meta.url), "utf8");

  assert.equal(source.includes("reportColumnFilters"), true);
  assert.equal(source.includes("toggleReportSort"), true);
  assert.equal(source.includes("Sort by"), true);
  assert.equal(apiSource.includes("filter."), true);
});

test("reports page reuses executed runtime options for CSV and report PDF output", () => {
  const source = readFileSync(new URL("./pages/ReportsPage.tsx", import.meta.url), "utf8");
  const apiSource = readFileSync(new URL("./api.ts", import.meta.url), "utf8");

  assert.equal(source.includes("executedReportRuntimeOptions"), true);
  assert.equal(source.includes("downloadReportPrintTemplatePdf("), true);
  assert.equal(source.includes("...executedReportRuntimeOptions"), true);
  assert.equal(source.includes("downloadListReportCsv(reportExecution.formId, reportExecution.reportId, executedReportRuntimeOptions)"), true);
  assert.equal(apiSource.includes("sortFieldId"), true);
  assert.equal(apiSource.includes("filter."), true);
});

test("reports page exposes record workflow actions from report rows", () => {
  const source = readFileSync(new URL("./pages/ReportsPage.tsx", import.meta.url), "utf8");
  const formsModuleSource = readFileSync(new URL("../../modules/forms/module.tsx", import.meta.url), "utf8");

  assert.equal(source.includes("deleteRecord"), true);
  assert.equal(source.includes("handleDeleteReportRecord"), true);
  assert.equal(source.includes("getRecordDetailPath"), true);
  assert.equal(source.includes("getRecordEditPath"), true);
  assert.equal(source.includes("getRecordCreatePath"), true);
  assert.equal(source.includes("New record"), true);
  assert.equal(source.includes("View"), true);
  assert.equal(source.includes("Edit"), true);
  assert.equal(source.includes("Delete"), true);
  assert.equal(formsModuleSource.includes('permission: ["menu.forms", "menu.reports"]'), true);
});

test("reports page exposes saved report edit duplicate and delete management", () => {
  const source = readFileSync(new URL("./pages/ReportsPage.tsx", import.meta.url), "utf8");
  const apiSource = readFileSync(new URL("./api.ts", import.meta.url), "utf8");

  assert.equal(apiSource.includes("getListReport"), true);
  assert.equal(apiSource.includes("updateListReport"), true);
  assert.equal(apiSource.includes("deleteListReport"), true);
  assert.equal(source.includes("editingReportId"), true);
  assert.equal(source.includes("handleEditReport"), true);
  assert.equal(source.includes("handleDuplicateReport"), true);
  assert.equal(source.includes("handleDeleteReportDefinition"), true);
  assert.equal(source.includes("Duplicate"), true);
  assert.equal(source.includes("Delete report"), true);
  assert.equal(source.includes("Save changes"), true);
});

test("reports page exposes inline saved report access controls", () => {
  const source = readFileSync(new URL("./pages/ReportsPage.tsx", import.meta.url), "utf8");
  const accessSource = readFileSync(new URL("./reportAccess.ts", import.meta.url), "utf8");

  assert.equal(source.includes("handleOpenReportAccess"), true);
  assert.equal(source.includes("handleSaveReportAccess"), true);
  assert.equal(source.includes("roles.manage"), true);
  assert.equal(source.includes("reportAccessMenuPermission"), true);
  assert.equal(accessSource.includes("menu.reports"), true);
  assert.equal(source.includes("Report access"), true);
  assert.equal(source.includes("Show Reports menu"), true);
  assert.equal(source.includes("updateRolePermissions"), true);
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

test("report builder preserves selected column order and custom labels", () => {
  const fields = [
    { id: "employee_name", label: "Employee name", type: "text", source: "form", options: [] },
    { id: "department", label: "Department", type: "select", source: "form", options: [] },
    { id: "created_at", label: "Created date", type: "datetime", source: "system", options: [] }
  ];

  const config = createListReportConfig({
    fieldOptions: fields,
    selectedFieldIds: ["created_at", "employee_name", "employee_name", "department"],
    columnLabels: {
      created_at: "Submitted",
      employee_name: "Employee",
      department: "Team"
    }
  });

  assert.deepEqual(config.columns.map((column) => column.fieldId), ["created_at", "employee_name", "department"]);
  assert.deepEqual(config.columns.map((column) => column.label), ["Submitted", "Employee", "Team"]);
  assert.equal(config.columns[0].width, 140);
});
