import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { test } from "vitest";
import * as api from "./api.ts";
import {
  createListReportConfig,
  defaultReportActions,
  defaultReportRowActions,
  getReportFieldOptions,
  getReportFilterOperatorOptions,
  getReportFilterValueInputType,
  getReportFilterValueOptions,
  normalizeReportActions,
  toListReportFilters,
  toListReportSorts,
  validateReportBuilderDrafts
} from "./builder.ts";

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

    if (input === "/api/forms/form-2/reports/fields" && init.method === "GET") {
      return {
        ok: true,
        json: async () => ({
          items: [{ id: "customer.name", label: "Customer › Name", type: "text", source: "relationship", options: [] }]
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

    if (input === "/api/forms/form-2/reports/report-1/run?page=2&pageSize=10&search=Jane&sortFieldId=site_name&sortDirection=asc&filter.customer.name=Warehouse" && init.method === "GET") {
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
      filters: { "customer.name": "Warehouse" }
    },
    fetcher
  );
  const fields = await api.listReportFields("form-2", fetcher);
  const exportUrl = api.getListReportCsvExportUrl("form-2", "report-1", {
    search: " Jane ",
    sortFieldId: "site_name",
    sortDirection: "asc",
    filters: { "customer.name": "Warehouse" }
  });
  let downloadedUrl = "";
  api.downloadListReportCsv(
    "form-2",
    "report-1",
    {
      search: " Jane ",
      sortFieldId: "site_name",
      sortDirection: "asc",
      filters: { "customer.name": "Warehouse" }
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
  assert.equal(fields[0].id, "customer.name");
  assert.equal(fields[0].source, "relationship");
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
  assert.equal(calls[5].input, "/api/forms/form-2/reports/report-1/run?page=2&pageSize=10&search=Jane&sortFieldId=site_name&sortDirection=asc&filter.customer.name=Warehouse");
  assert.equal(calls[5].init.method, "GET");
  assert.equal(calls[5].init.credentials, "include");
  assert.equal(calls[6].input, "/api/forms/form-2/reports/fields");
  assert.equal(calls[6].init.method, "GET");
  assert.equal(exportUrl, "/api/forms/form-2/reports/report-1/export.csv?search=Jane&sortFieldId=site_name&sortDirection=asc&filter.customer.name=Warehouse");
  assert.equal(downloadedUrl, "/api/forms/form-2/reports/report-1/export.csv?search=Jane&sortFieldId=site_name&sortDirection=asc&filter.customer.name=Warehouse");

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
  const builderSource = readFileSync(new URL("./builder.ts", import.meta.url), "utf8");
  const formsModuleSource = readFileSync(new URL("../../modules/forms/module.tsx", import.meta.url), "utf8");

  assert.equal(source.includes("onOpenRecord"), true);
  assert.equal(source.includes("Open record detail"), true);
  assert.equal(source.includes('role={rowCanOpen ? "button" : undefined}'), true);
  assert.equal(source.includes("ReportRowActionMenu"), true);
  assert.equal(source.includes("MoreHorizontal"), true);
  assert.equal(source.includes("Row actions"), true);
  assert.equal(source.includes("deleteRecord"), true);
  assert.equal(source.includes("handleDeleteReportRecord"), true);
  assert.equal(source.includes("reportExecution?.reportActions.map"), true);
  assert.equal(source.includes("actions.map((action)"), true);
  assert.equal(source.includes("OperationalActionEditor"), true);
  assert.equal(source.includes("getRecordDetailPath"), true);
  assert.equal(source.includes("getRecordEditPath"), true);
  assert.equal(source.includes("getRecordCreatePath"), true);
  assert.equal(builderSource.includes("New record"), true);
  assert.equal(source.includes("View"), true);
  assert.equal(source.includes("Edit"), true);
  assert.equal(source.includes("Delete"), true);
  assert.equal(formsModuleSource.includes('permission: ["menu.forms", "menu.reports"]'), true);
});

test("reports page exposes saved row open behavior settings", () => {
  const source = readFileSync(new URL("./pages/ReportsPage.tsx", import.meta.url), "utf8");

  assert.equal(source.includes("rowOpenAction"), true);
  assert.equal(source.includes("Row click opens"), true);
  assert.equal(source.includes("openRecordFromReportRow"), true);
  assert.equal(source.includes('value="none"'), true);
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

test("reports page exposes multiple saved filters and sorts in the builder", () => {
  const source = readFileSync(new URL("./pages/ReportsPage.tsx", import.meta.url), "utf8");

  assert.equal(source.includes("filterDrafts"), true);
  assert.equal(source.includes("sortDrafts"), true);
  assert.equal(source.includes("handleAddFilterDraft"), true);
  assert.equal(source.includes("handleRemoveFilterDraft"), true);
  assert.equal(source.includes("handleAddSortDraft"), true);
  assert.equal(source.includes("handleRemoveSortDraft"), true);
  assert.equal(source.includes("Add filter"), true);
  assert.equal(source.includes("Add sort"), true);
});

test("reports page validates saved filter and sort drafts before save", () => {
  const source = readFileSync(new URL("./pages/ReportsPage.tsx", import.meta.url), "utf8");

  assert.equal(source.includes("reportBuilderValidation"), true);
  assert.equal(source.includes("showReportBuilderValidation"), true);
  assert.equal(source.includes("filterErrorsById"), true);
  assert.equal(source.includes("sortErrorsById"), true);
  assert.equal(source.includes("Fix the highlighted report builder fields before saving."), true);
});

test("reports page exposes type-aware saved filter controls", () => {
  const source = readFileSync(new URL("./pages/ReportsPage.tsx", import.meta.url), "utf8");

  assert.equal(source.includes("getReportFilterOperatorOptions"), true);
  assert.equal(source.includes("getReportFilterValueInputType"), true);
  assert.equal(source.includes("getReportFilterValueOptions"), true);
  assert.equal(source.includes("Choose value"), true);
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
  assert.equal(createListReportConfig({ fieldOptions: fields, selectedFieldIds: ["department"] }).rowOpenAction, "detail");
  assert.deepEqual(createListReportConfig({ fieldOptions: fields, selectedFieldIds: ["department"] }).reportActions, defaultReportActions);
  assert.deepEqual(createListReportConfig({ fieldOptions: fields, selectedFieldIds: ["department"] }).rowActions, defaultReportRowActions);
});

test("report builder normalizes typed operational action editor state", () => {
  assert.deepEqual(normalizeReportActions(undefined, "report"), defaultReportActions);
  assert.deepEqual(normalizeReportActions([], "row"), defaultReportRowActions.map((action) => ({ ...action, enabled: false })));

  const configured = normalizeReportActions([
    { id: "remove", type: "delete_record", label: " Remove ", enabled: true, confirmation: " Remove this? " },
    { id: "open", type: "view_record", label: "Open", enabled: true }
  ], "row");

  assert.deepEqual(configured.map((action) => action.type), ["delete_record", "view_record", "edit_record"]);
  assert.equal(configured[0].label, "Remove");
  assert.equal(configured[0].confirmation, "Remove this?");
  assert.equal(configured[2].enabled, false);
});

test("report builder returns type-aware filter operators and value controls", () => {
  const numberField = { id: "salary", label: "Salary", type: "number", source: "form", options: [] };
  const dateField = { id: "created_at", label: "Created date", type: "datetime", source: "system", options: [] };
  const choiceField = {
    id: "department",
    label: "Department",
    type: "select",
    source: "form",
    options: [
      { id: "opt_hr", label: "Human Resources", value: "hr" },
      { id: "opt_finance", label: "Finance", value: "finance" }
    ]
  };

  assert.deepEqual(getReportFilterOperatorOptions(numberField).map((option) => option.value), [
    "equals",
    "greater_than",
    "greater_or_equal",
    "less_than",
    "less_or_equal",
    "is_empty",
    "is_not_empty"
  ]);
  assert.deepEqual(getReportFilterOperatorOptions(dateField).map((option) => option.value), ["equals", "before", "after", "is_empty", "is_not_empty"]);
  assert.deepEqual(getReportFilterOperatorOptions(choiceField).map((option) => option.value), ["equals", "is_empty", "is_not_empty"]);
  assert.equal(getReportFilterValueInputType(numberField), "number");
  assert.equal(getReportFilterValueInputType(dateField), "datetime-local");
  assert.deepEqual(getReportFilterValueOptions(choiceField), [
    { label: "Human Resources", value: "hr" },
    { label: "Finance", value: "finance" }
  ]);
});

test("report builder converts multiple filter and sort drafts into config arrays", () => {
  const filters = toListReportFilters([
    { id: "filter-1", fieldId: "department", operator: "equals", value: " HR " },
    { id: "filter-2", fieldId: "status", operator: "is_not_empty", value: "ignored" },
    { id: "filter-empty", fieldId: "", operator: "contains", value: "skip" }
  ]);
  const sorts = toListReportSorts([
    { id: "sort-1", fieldId: "created_at", direction: "desc" },
    { id: "sort-2", fieldId: "department", direction: "asc" },
    { id: "sort-empty", fieldId: "", direction: "asc" }
  ]);

  assert.deepEqual(filters, [
    { fieldId: "department", operator: "equals", value: "HR" },
    { fieldId: "status", operator: "is_not_empty", value: null }
  ]);
  assert.deepEqual(sorts, [
    { fieldId: "created_at", direction: "desc" },
    { fieldId: "department", direction: "asc" }
  ]);
});

test("report builder validates active filters and duplicate sorts", () => {
  const fieldOptions = [
    { id: "department", label: "Department", type: "select", source: "form", options: [] },
    {
      id: "team",
      label: "Team",
      type: "select",
      source: "form",
      options: [{ id: "opt_hr", label: "Human Resources", value: "hr" }]
    },
    { id: "salary", label: "Salary", type: "number", source: "form", options: [] },
    { id: "created_at", label: "Created date", type: "datetime", source: "system", options: [] },
    { id: "start_time", label: "Start time", type: "time", source: "form", options: [] }
  ];

  const validation = validateReportBuilderDrafts({
    fieldOptions,
    filterDrafts: [
      { id: "filter-empty", fieldId: "", operator: "equals", value: "" },
      { id: "filter-missing", fieldId: "department", operator: "equals", value: " " },
      { id: "filter-unknown", fieldId: "missing_field", operator: "is_not_empty", value: "" },
      { id: "filter-unsupported", fieldId: "department", operator: "greater_than", value: "10" },
      { id: "filter-number", fieldId: "salary", operator: "greater_than", value: "abc" },
      { id: "filter-date", fieldId: "created_at", operator: "after", value: "not-a-date" },
      { id: "filter-time", fieldId: "start_time", operator: "before", value: "99:99" },
      { id: "filter-choice", fieldId: "team", operator: "equals", value: "finance" }
    ],
    sortDrafts: [
      { id: "sort-created-a", fieldId: "created_at", direction: "desc" },
      { id: "sort-created-b", fieldId: "created_at", direction: "asc" },
      { id: "sort-empty", fieldId: "", direction: "asc" },
      { id: "sort-unknown", fieldId: "missing_field", direction: "desc" }
    ]
  });

  assert.equal(validation.isValid, false);
  assert.equal(validation.filterErrorsById["filter-empty"], undefined);
  assert.equal(validation.filterErrorsById["filter-missing"].value, "Filter value is required.");
  assert.equal(validation.filterErrorsById["filter-unknown"].fieldId, "Filter field is not available.");
  assert.equal(validation.filterErrorsById["filter-unsupported"].operator, "Filter operator is not available for this field.");
  assert.equal(validation.filterErrorsById["filter-number"].value, "Filter value must be a number.");
  assert.equal(validation.filterErrorsById["filter-date"].value, "Filter value must be a valid date/time.");
  assert.equal(validation.filterErrorsById["filter-time"].value, "Filter value must be a valid time.");
  assert.equal(validation.filterErrorsById["filter-choice"].value, "Choose an available filter value.");
  assert.equal(validation.sortErrorsById["sort-created-a"].fieldId, "Sort field is already used.");
  assert.equal(validation.sortErrorsById["sort-created-b"].fieldId, "Sort field is already used.");
  assert.equal(validation.sortErrorsById["sort-empty"].fieldId, "Sort field is required.");
  assert.equal(validation.sortErrorsById["sort-unknown"].fieldId, "Sort field is not available.");
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
