import assert from "node:assert/strict";
import { test } from "vitest";
import { instantiateDashboardTemplate, validateDashboardTemplate } from "./templateEngine.ts";
import { businessPerformanceSampleTemplate } from "./templates/businessPerformanceSample.ts";
import { operationsPerformanceTemplate } from "./templates/operationsPerformance.ts";
import { createDashboardTemplateCatalog, dashboardTemplateCatalog, validateTemplateFieldCapabilities } from "./templates/catalog.ts";

const formId = "11000000-0000-0000-0000-000000000001";
const operationsFormId = "11000000-0000-0000-0000-000000000011";
const incidentFormId = "11000000-0000-0000-0000-000000000021";
const sources = { business: { formId }, operations: { formId: operationsFormId }, incidents: { formId: incidentFormId } };

test("Business Performance template creates an independent eleven-section multi-source draft", () => {
  let sequence = 0;
  const instantiate = () => instantiateDashboardTemplate(
    businessPerformanceSampleTemplate,
    { sources },
    { idGenerator: () => `id-${++sequence}`, now: () => "2026-08-21T12:00:00.000Z", availableAdapterIds: new Set(["sample-dashboard"]) }
  );
  const first = instantiate();
  const second = instantiate();
  assert.equal(first.ok, true);
  assert.equal(second.ok, true);
  if (!first.ok || !second.ok) return;
  assert.equal(first.dashboard.config.sections.length, 11);
  assert.equal(first.dashboard.config.widgets.length, businessPerformanceSampleTemplate.widgets.length);
  assert.equal(first.dashboard.config.filters.length, 8);
  assert.equal(first.dashboard.config.filters[0].sourceFormId, formId);
  assert.equal(first.dashboard.config.filters[4].sourceFormId, operationsFormId);
  assert.equal(first.dashboard.config.filters[7].sourceFormId, incidentFormId);
  assert.equal(first.dashboard.publication.status, "draft");
  assert.equal(first.dashboard.config.templateProvenance.templateId, "business-performance-sample");
  assert.notEqual(first.dashboard.config.widgets[0].id, second.dashboard.config.widgets[0].id);
  first.dashboard.config.widgets[0].title = "Changed instance";
  assert.equal(second.dashboard.config.widgets[0].title, "Total records");
  assert.equal(businessPerformanceSampleTemplate.widgets[0].title, "Total records");
});

test("template instantiation deep-clones KPI conditional rules", () => {
  const template = { ...businessPerformanceSampleTemplate, widgets: businessPerformanceSampleTemplate.widgets.map((widget, index) => index !== 0 || widget.source.kind !== "analytics" ? widget : { ...widget, source: { ...widget.source, chart: { ...widget.source.chart, appearance: { palette: "theme", showLegend: true, showDataLabels: false, showGridlines: true, cardAccent: "none", numberFormat: "auto", currencyCode: "CAD", decimalPlaces: 0, conditionalFormatting: { enabled: true, rules: [{ id: "target", operator: "greater_or_equal", value: 40, accent: "success" }] } } } } }) };
  let sequence = 0;
  const instantiate = () => instantiateDashboardTemplate(template, { sources }, { idGenerator: () => `conditional-${++sequence}`, availableAdapterIds: new Set(["sample-dashboard"]) });
  const first = instantiate();
  const second = instantiate();
  assert.equal(first.ok, true);
  assert.equal(second.ok, true);
  if (!first.ok || !second.ok) return;
  first.dashboard.config.widgets[0].chart.appearance.conditionalFormatting.rules[0].value = 50;
  assert.equal(second.dashboard.config.widgets[0].chart.appearance.conditionalFormatting.rules[0].value, 40);
  assert.equal(template.widgets[0].source.chart.appearance.conditionalFormatting.rules[0].value, 40);
});

test("Operations Performance template creates an independent seven-section draft", () => {
  let sequence = 0;
  const instantiate = () => instantiateDashboardTemplate(
    operationsPerformanceTemplate,
    { sources: { operations: { formId: operationsFormId } } },
    { idGenerator: () => `operations-${++sequence}`, now: () => "2026-08-25T12:00:00.000Z", availableAdapterIds: new Set(["sample-dashboard"]) }
  );
  const first = instantiate();
  const second = instantiate();
  assert.equal(first.ok, true);
  assert.equal(second.ok, true);
  if (!first.ok || !second.ok) return;
  assert.equal(first.dashboard.config.sections.length, 7);
  assert.equal(first.dashboard.config.widgets.length, 24);
  assert.equal(first.dashboard.config.filters.length, 5);
  assert.equal(first.dashboard.config.templateProvenance?.templateId, "operations-performance");
  assert.equal(first.dashboard.config.templateProvenance?.templateVersion, 2);
  assert.equal(first.dashboard.publication.status, "draft");
  assert.equal(first.dashboard.settings.visibility, "workspace");
  assert.notEqual(first.dashboard.config.widgets[0].id, second.dashboard.config.widgets[0].id);
  first.dashboard.config.widgets[0].title = "Changed instance";
  assert.equal(second.dashboard.config.widgets[0].title, "Operational facts");
  assert.equal(operationsPerformanceTemplate.widgets[0].title, "Operational facts");
  const moduleBySection = new Map([["loss", "Loss"], ["production", "Production"], ["engineering", "Engineering"], ["supply-chain", "Supply Chain"], ["qaqc", "QAQC"]]);
  for (const widget of operationsPerformanceTemplate.widgets.filter((item) => item.source.kind === "analytics" && moduleBySection.has(item.sectionKey))) {
    assert.deepEqual(widget.source.chart.fixedFilters, [{ fieldId: "module", values: [moduleBySection.get(widget.sectionKey)] }]);
  }
});

test("Operations Performance filters resolve only to intended widget ids", () => {
  let sequence = 0;
  const result = instantiateDashboardTemplate(
    operationsPerformanceTemplate,
    { sources: { operations: { formId: operationsFormId } } },
    { idGenerator: () => `target-${++sequence}`, availableAdapterIds: new Set(["sample-dashboard"]) }
  );
  assert.equal(result.ok, true);
  if (!result.ok) return;
  const widgetsByTitle = new Map(result.dashboard.config.widgets.map((widget) => [widget.title, widget.id]));
  const product = result.dashboard.config.filters.find((filter) => filter.label === "Product / recipe");
  const equipment = result.dashboard.config.filters.find((filter) => filter.label === "Equipment");
  const module = result.dashboard.config.filters.find((filter) => filter.label === "Module");
  assert.deepEqual(product?.applyToWidgetIds, [
    widgetsByTitle.get("Production by product"), widgetsByTitle.get("Production trend"),
    widgetsByTitle.get("Inventory by product"), widgetsByTitle.get("Supply-chain KPI families"),
    widgetsByTitle.get("QA/QC first-time release"), widgetsByTitle.get("Quality metrics"),
    widgetsByTitle.get("Quality detail"), widgetsByTitle.get("Operational detail")
  ]);
  assert.deepEqual(equipment?.applyToWidgetIds, [
    widgetsByTitle.get("Engineering performance"), widgetsByTitle.get("Utilities and reliability trend"),
    widgetsByTitle.get("Operational detail")
  ]);
  assert.deepEqual(module?.applyToWidgetIds, [
    widgetsByTitle.get("Operational facts"), widgetsByTitle.get("Total actual"),
    widgetsByTitle.get("Total target"), widgetsByTitle.get("Performance by module"),
    widgetsByTitle.get("Operational actual over time"), widgetsByTitle.get("Operational detail")
  ]);
});

test("Operations Performance template validates source capabilities and adapter availability", () => {
  const fields = new Set([
    "module", "metric_key", "fiscal_year", "period_type", "period_label", "period_number", "period_date",
    "product", "equipment", "actual_value", "target_value", "budget_value", "numerator", "denominator", "unit", "status"
  ]);
  assert.deepEqual(validateDashboardTemplate(operationsPerformanceTemplate), []);
  assert.deepEqual(validateTemplateFieldCapabilities(operationsPerformanceTemplate, { operations: fields }), []);
  assert.ok(validateTemplateFieldCapabilities(operationsPerformanceTemplate, { operations: new Set(["status"]) }).length > 0);
  const unavailable = instantiateDashboardTemplate(operationsPerformanceTemplate, { sources: { operations: { formId: operationsFormId } } }, { availableAdapterIds: new Set() });
  assert.equal(unavailable.ok, false);
  if (!unavailable.ok) assert.ok(unavailable.errors.some((error) => error.code === "template.adapter.unavailable"));
  assert.ok(dashboardTemplateCatalog.some((template) => template.id === "operations-performance"));
});

test("template instantiation validates source bindings and ids", () => {
  const missing = instantiateDashboardTemplate(businessPerformanceSampleTemplate, { sources: {} });
  assert.equal(missing.ok, false);
  if (!missing.ok) assert.equal(missing.errors[0].code, "template.binding.required");

  const malformed = instantiateDashboardTemplate(businessPerformanceSampleTemplate, { sources: { ...sources, business: { formId: "not-a-guid" } } });
  assert.equal(malformed.ok, false);
  if (!malformed.ok) assert.ok(malformed.errors.some((error) => error.code === "template.binding.form_id_invalid"));

  const unknown = instantiateDashboardTemplate(businessPerformanceSampleTemplate, { sources: { ...sources, extra: { formId } } });
  assert.equal(unknown.ok, false);
  if (!unknown.ok) assert.ok(unknown.errors.some((error) => error.code === "template.binding.unknown"));
});

test("template validation rejects duplicate and stale structure references", () => {
  const invalid = {
    ...businessPerformanceSampleTemplate,
    sections: [...businessPerformanceSampleTemplate.sections, businessPerformanceSampleTemplate.sections[0]],
    widgets: [{ ...businessPerformanceSampleTemplate.widgets[0], sectionKey: "missing", source: { ...businessPerformanceSampleTemplate.widgets[0].source, sourceSlot: "missing" } }]
  };
  const errors = validateDashboardTemplate(invalid);
  assert.ok(errors.some((error) => error.code === "template.sections.duplicate"));
  assert.ok(errors.some((error) => error.code === "template.widget.section_missing"));
  assert.ok(errors.some((error) => error.code === "template.widget.source_missing"));
});

test("template validation rejects mixed analytics and adapter sources", () => {
  const invalid = {
    ...businessPerformanceSampleTemplate,
    widgets: [{ ...businessPerformanceSampleTemplate.widgets[0], source: { ...businessPerformanceSampleTemplate.widgets[0].source, adapter: { adapterId: "x", visualizationId: "x", settings: {} } } }]
  };
  assert.ok(validateDashboardTemplate(invalid).some((error) => error.code === "template.widget.source_ambiguous"));
});

test("template catalog is deterministic and rejects duplicate ids", () => {
  const other = { ...businessPerformanceSampleTemplate, id: "another", name: "Another" };
  assert.deepEqual(createDashboardTemplateCatalog([businessPerformanceSampleTemplate, other]).map((item) => item.id), ["another", "business-performance-sample"]);
  assert.throws(() => createDashboardTemplateCatalog([businessPerformanceSampleTemplate, businessPerformanceSampleTemplate]), /already registered/);
});

test("sample capability validation explains missing reportable fields", () => {
  const available = {
    business: new Set(["amount", "event_date", "category", "region", "priority", "status", "created_at", "title"]),
    operations: new Set(["actual_value", "target_value", "module", "metric_key", "fiscal_year", "period_type", "period_label", "period_date", "product", "equipment", "unit", "status"]),
    incidents: new Set(["incident_cost", "lost_hours", "location", "incident_date"])
  };
  assert.deepEqual(validateTemplateFieldCapabilities(businessPerformanceSampleTemplate, available), []);
  const errors = validateTemplateFieldCapabilities(businessPerformanceSampleTemplate, { ...available, business: new Set(["status", "created_at"]) });
  assert.ok(errors.some((error) => error.message.includes("amount")));
});

test("template instantiation fails safely when its bounded adapter is unavailable", () => {
  const result = instantiateDashboardTemplate(businessPerformanceSampleTemplate, { sources }, { availableAdapterIds: new Set() });
  assert.equal(result.ok, false);
  if (!result.ok) assert.ok(result.errors.some((error) => error.code === "template.adapter.unavailable"));
});
