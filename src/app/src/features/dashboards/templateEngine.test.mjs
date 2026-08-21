import assert from "node:assert/strict";
import { test } from "vitest";
import { instantiateDashboardTemplate, validateDashboardTemplate } from "./templateEngine.ts";
import { businessPerformanceSampleTemplate } from "./templates/businessPerformanceSample.ts";
import { createDashboardTemplateCatalog, validateTemplateFieldCapabilities } from "./templates/catalog.ts";

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
