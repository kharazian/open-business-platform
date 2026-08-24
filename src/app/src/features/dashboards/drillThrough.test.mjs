import assert from "node:assert/strict";
import { test } from "vitest";
import { buildDashboardDrillThroughPath, readDashboardDrillFilters } from "./drillThrough.ts";

const widget = { id: "widget-1", title: "By region", sourceFormId: "form-1", chart: { widgetType: "choice_breakdown", metric: { type: "count" }, groupByFieldId: "region" }, interaction: { destination: "records", includeDashboardFilters: true, includePointFilter: true } };
const definitions = [{ id: "status", label: "Status", type: "single_select", sourceFormId: "form-1", fieldId: "status" }];

test("drill-through builds typed record and report destinations with bounded scalar filters", () => {
  const path = buildDashboardDrillThroughPath(widget, definitions, { status: { fieldId: "status", values: ["active"] } }, { key: "North", label: "North", value: 4 });
  assert.ok(path.startsWith("/forms/form-1/records?"));
  const query = new URLSearchParams(path.split("?")[1]);
  assert.deepEqual(readDashboardDrillFilters(query), { status: "active", region: "North" });
  const reportPath = buildDashboardDrillThroughPath({ ...widget, interaction: { destination: "report", reportId: "report-1" } }, [], {}, undefined);
  assert.match(reportPath, /^\/reports\?/);
});

test("table row selections open typed record detail destinations", () => {
  assert.equal(buildDashboardDrillThroughPath(widget, [], {}, { key: "record-1", label: "Record", value: 0, recordId: "record-1" }), "/records/record-1");
});
