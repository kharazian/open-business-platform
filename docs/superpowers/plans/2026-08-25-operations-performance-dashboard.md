# Operations Performance Dashboard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a reusable Operations Performance dashboard template and a separately seeded, published Operations Performance Sample dashboard backed by the existing 72-record Operations form.

**Architecture:** Define an environment-neutral TypeScript template with one `operations` source slot, then create a semantically equivalent C# seed definition bound to the deterministic Operations form. Both paths reuse existing dashboard analytics, bounded sample adapters, targeted filters, permissions, validation, persistence, and publication snapshots; they do not share runtime code across languages or add a new module.

**Tech Stack:** React, TypeScript, Vitest, ASP.NET Core/.NET 10, EF Core, PostgreSQL, existing dashboard analytics and `sample-dashboard` adapter.

**Spec:** `docs/superpowers/specs/2026-08-25-operations-performance-dashboard-design.md`

## Global Constraints

- Keep the frontend template free of environment-specific form or report IDs.
- Use the existing `Operational Performance Sample Data` form and its 72 deterministic records.
- Use analytics widgets for record-backed values and adapters only for bounded illustrative visual treatments.
- Use exactly seven sections and five shared filters; stay below 48 widgets and eight filters.
- Use template ID `operations-performance`, template version `1`, and published slug `operations-performance-sample`.
- The seeded dashboard is workspace-visible, not the workspace default, and not shown in navigation.
- Seeding must be additive: never overwrite, republish, unarchive, or duplicate an existing deterministic dashboard.
- Preserve backend form/report/record-scope/hidden-field permission enforcement.
- Add no database migration, chart dependency, route family, or Operations module.
- Frontend commands require Node.js `>=20.19.0`; backend commands require .NET 10.

---

### Task 1: Add the reusable Operations Performance template

**Files:**

- Create: `src/app/src/features/dashboards/templates/operationsPerformance.ts`
- Modify: `src/app/src/features/dashboards/templates/catalog.ts`
- Modify: `src/app/src/features/dashboards/templateEngine.test.mjs`

**Interfaces:**

- Consumes: `DashboardTemplateDefinition`, `instantiateDashboardTemplate()`, `validateDashboardTemplate()`, `validateTemplateFieldCapabilities()`, and the registered `sample-dashboard` adapter.
- Produces: `operationsPerformanceTemplate: DashboardTemplateDefinition`, registered under stable ID `operations-performance`.

- [ ] **Step 1: Write failing template structure and instantiation tests**

Import the new template and catalog, then add tests that require seven sections, 24 widgets, five filters, one source slot, provenance version 1, targeted filter IDs, independent instances, complete field capability validation, and adapter availability:

```ts
import { operationsPerformanceTemplate } from "./templates/operationsPerformance.ts";
import { dashboardTemplateCatalog } from "./templates/catalog.ts";

const operationsSources = { operations: { formId: operationsFormId } };

test("Operations Performance template creates an independent seven-section draft", () => {
  let sequence = 0;
  const instantiate = () => instantiateDashboardTemplate(
    operationsPerformanceTemplate,
    { sources: operationsSources },
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
  assert.equal(first.dashboard.config.templateProvenance?.templateVersion, 1);
  assert.equal(first.dashboard.publication.status, "draft");
  assert.equal(first.dashboard.settings.visibility, "workspace");
  assert.notEqual(first.dashboard.config.widgets[0].id, second.dashboard.config.widgets[0].id);
  first.dashboard.config.widgets[0].title = "Changed instance";
  assert.equal(second.dashboard.config.widgets[0].title, "Operational facts");
  assert.equal(operationsPerformanceTemplate.widgets[0].title, "Operational facts");
});

test("Operations Performance filters resolve only to intended widget ids", () => {
  let sequence = 0;
  const result = instantiateDashboardTemplate(
    operationsPerformanceTemplate,
    { sources: operationsSources },
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
  const unavailable = instantiateDashboardTemplate(operationsPerformanceTemplate, { sources: operationsSources }, { availableAdapterIds: new Set() });
  assert.equal(unavailable.ok, false);
  if (!unavailable.ok) assert.ok(unavailable.errors.some((error) => error.code === "template.adapter.unavailable"));
  assert.ok(dashboardTemplateCatalog.some((template) => template.id === "operations-performance"));
});
```

- [ ] **Step 2: Run the focused test and confirm the expected failure**

Run:

```bash
cd src/app
npx vitest run src/features/dashboards/templateEngine.test.mjs
```

Expected: FAIL because `./templates/operationsPerformance.ts` does not exist.

- [ ] **Step 3: Implement the template definition**

Create `operationsPerformance.ts` with one form slot, these seven sections, and the exact 24 widget keys below. Use the same `analytics()` and `adapter()` helper shape as `businessPerformanceSample.ts`.

```ts
import type { DashboardTemplateDefinition } from "../templateEngine";

type AnalyticsOptions = {
  metricFieldId?: string;
  groupByFieldId?: string;
  dateFieldId?: string;
  columns?: string[];
  limit?: number;
  subtitle?: string;
};

export const operationsPerformanceTemplate: DashboardTemplateDefinition = {
  id: "operations-performance",
  version: 1,
  name: "Operations Performance",
  description: "A focused operations dashboard covering overview, loss, production, engineering, supply chain, QA/QC, trends, and record detail.",
  category: "Operations",
  tags: ["operations", "loss", "production", "engineering", "supply-chain", "qaqc", "starter"],
  requiredAdapterIds: ["sample-dashboard"],
  sourceSlots: [{
    key: "operations", label: "Operational performance data",
    description: "Period-based operational facts for loss, production, engineering, supply chain, QA/QC, targets, products, and equipment.",
    kind: "form", required: true, allowReport: true
  }],
  sections: [
    { key: "overview", title: "Operations Overview", icon: "gauge" },
    { key: "loss", title: "Loss", icon: "trending-up" },
    { key: "production", title: "Production", icon: "factory" },
    { key: "engineering", title: "Engineering", icon: "wrench" },
    { key: "supply-chain", title: "Supply Chain", icon: "package-check" },
    { key: "qaqc", title: "QA/QC", icon: "shield-check" },
    { key: "trends-records", title: "Trends & Records", icon: "clipboard-list" }
  ],
  filters: [
    { key: "fiscal-year", label: "Fiscal year", type: "single_select", sourceSlot: "operations", fieldId: "fiscal_year", options: ["2025", "2026"] },
    { key: "period-type", label: "Period", type: "single_select", sourceSlot: "operations", fieldId: "period_type", options: ["Week", "Month", "Quarter"] },
    { key: "product", label: "Product / recipe", type: "multi_select", sourceSlot: "operations", fieldId: "product", options: ["Classic", "Premium", "Light", "Specialty"], applyToWidgetKeys: ["production-by-product", "production-trend", "supply-by-product", "supply-by-metric", "qaqc-rate", "qaqc-metrics", "qaqc-detail", "recent-operations"] },
    { key: "equipment", label: "Equipment", type: "multi_select", sourceSlot: "operations", fieldId: "equipment", options: ["Line 1", "Line 2", "Dryer", "Packaging"], applyToWidgetKeys: ["engineering-by-equipment", "engineering-trend", "recent-operations"] },
    { key: "module", label: "Module", type: "single_select", sourceSlot: "operations", fieldId: "module", options: ["Loss", "Production", "Engineering", "Supply Chain", "QAQC"], applyToWidgetKeys: ["operational-facts", "total-actual", "total-target", "performance-by-module", "operations-over-time", "recent-operations"] }
  ],
  widgets: [
    analytics("operational-facts", "Operational facts", "overview", "small", "number_card", "count"),
    analytics("total-actual", "Total actual", "overview", "small", "number_card", "sum", { metricFieldId: "actual_value" }),
    analytics("total-target", "Total target", "overview", "small", "number_card", "sum", { metricFieldId: "target_value" }),
    analytics("performance-by-module", "Performance by module", "overview", "wide", "choice_breakdown", "average", { metricFieldId: "actual_value", groupByFieldId: "module" }),
    adapter("overview-target", "Actual versus target", "overview", "wide", "target_attainment", { actual: 92, target: 100, unit: "%", tone: "warning", sourceLabel: "Illustrative Operations sample adapter" }),
    analytics("loss-actual", "Total loss actual", "loss", "small", "number_card", "sum", { metricFieldId: "actual_value", subtitle: "Use Module to narrow the permitted operational facts" }),
    analytics("loss-by-metric", "Loss by metric", "loss", "wide", "choice_breakdown", "sum", { metricFieldId: "actual_value", groupByFieldId: "metric_key" }),
    adapter("loss-target", "Loss actual and standard", "loss", "wide", "combo", { labels: "Jan|Feb|Mar|Apr|May|Jun", primary: "8|7|9|6|5|6", secondary: "7|7|7|6|6|6", unit: "%", sourceLabel: "Illustrative Operations sample adapter" }),
    analytics("production-by-product", "Production by product", "production", "wide", "choice_breakdown", "sum", { metricFieldId: "actual_value", groupByFieldId: "product" }),
    analytics("production-trend", "Production trend", "production", "wide", "date_trend", "sum", { metricFieldId: "actual_value", dateFieldId: "period_date" }),
    adapter("production-stack", "Product composition", "production", "wide", "stacked_bar", { labels: "Q1|Q2|Q3|Q4", primary: "42|48|51|55", secondary: "31|34|38|41", tertiary: "18|21|24|27", unit: "t", sourceLabel: "Illustrative Operations sample adapter" }),
    analytics("engineering-by-equipment", "Engineering performance", "engineering", "wide", "choice_breakdown", "average", { metricFieldId: "actual_value", groupByFieldId: "equipment" }),
    analytics("engineering-trend", "Utilities and reliability trend", "engineering", "wide", "date_trend", "average", { metricFieldId: "actual_value", dateFieldId: "period_date" }),
    adapter("engineering-target", "Actual versus engineering standard", "engineering", "wide", "target_line", { labels: "W1|W2|W3|W4|W5|W6", primary: "72|69|75|71|68|66", secondary: "70|70|70|70|70|70", unit: "%", sourceLabel: "Illustrative Operations sample adapter" }),
    analytics("supply-by-product", "Inventory by product", "supply-chain", "wide", "choice_breakdown", "sum", { metricFieldId: "actual_value", groupByFieldId: "product" }),
    analytics("supply-by-metric", "Supply-chain KPI families", "supply-chain", "wide", "choice_breakdown", "average", { metricFieldId: "actual_value", groupByFieldId: "metric_key" }),
    adapter("supply-attainment", "Service-level attainment", "supply-chain", "medium", "target_attainment", { actual: 96, target: 98, unit: "%", tone: "warning", sourceLabel: "Illustrative Operations sample adapter" }),
    analytics("qaqc-rate", "QA/QC first-time release", "qaqc", "small", "number_card", "average", { metricFieldId: "actual_value" }),
    analytics("qaqc-metrics", "Quality metrics", "qaqc", "wide", "choice_breakdown", "average", { metricFieldId: "actual_value", groupByFieldId: "metric_key" }),
    analytics("qaqc-detail", "Quality detail", "qaqc", "full", "table", "count", { columns: ["period_label", "metric_key", "product", "actual_value", "target_value", "unit", "status"], limit: 20 }),
    analytics("operations-over-time", "Operational actual over time", "trends-records", "wide", "date_trend", "sum", { metricFieldId: "actual_value", dateFieldId: "period_date" }),
    adapter("actual-budget", "Actual and budget comparison", "trends-records", "wide", "combo", { labels: "Jan|Feb|Mar|Apr|May|Jun", primary: "31|35|39|42|46|49", secondary: "30|34|38|43|45|48", unit: "%", sourceLabel: "Illustrative Operations sample adapter" }),
    analytics("recent-operations", "Operational detail", "trends-records", "full", "table", "count", { columns: ["module", "metric_key", "period_label", "period_number", "product", "equipment", "actual_value", "target_value", "budget_value", "numerator", "denominator", "unit"], limit: 20 }),
    adapter("detail-popup", "Period detail preview", "trends-records", "wide", "detail_popup", { title: "Selected period detail", period: "2026 Q2", rows: 18, groups: "Module|Product|Equipment|Metric", sourceLabel: "Illustrative Operations sample adapter" })
  ]
};
```

Add these helpers below the template. They fix the source slot to `operations` and retain independent settings objects:

```ts
function analytics(
  key: string,
  title: string,
  sectionKey: string,
  width: "small" | "medium" | "wide" | "full",
  widgetType: "number_card" | "choice_breakdown" | "date_trend" | "table",
  metricType: "count" | "sum" | "average",
  options: AnalyticsOptions = {}
) {
  return {
    key, title, subtitle: options.subtitle, sectionKey, width,
    source: {
      kind: "analytics" as const,
      sourceSlot: "operations",
      chart: {
        widgetType,
        metric: { type: metricType, fieldId: options.metricFieldId ?? null },
        groupByFieldId: options.groupByFieldId ?? null,
        dateFieldId: options.dateFieldId ?? null,
        columns: options.columns ?? [],
        limit: options.limit ?? 12
      }
    }
  };
}

function adapter(
  key: string,
  title: string,
  sectionKey: string,
  width: "small" | "medium" | "wide" | "full",
  visualizationId: string,
  settings: Record<string, string | number | boolean | null>
) {
  return {
    key, title, sectionKey, width,
    source: { kind: "adapter" as const, adapter: { adapterId: "sample-dashboard", visualizationId, settings } }
  };
}
```

Do not add static per-widget query filters because that is not part of the existing analytics contract; module-specific copy must remain honest and the Module filter provides explicit narrowing.

- [ ] **Step 4: Register the template**

Modify `catalog.ts`:

```ts
import { operationsPerformanceTemplate } from "./operationsPerformance";

export const dashboardTemplateCatalog = createDashboardTemplateCatalog([
  businessPerformanceSampleTemplate,
  operationsPerformanceTemplate
]);
```

- [ ] **Step 5: Run focused and complete frontend tests**

Run:

```bash
cd src/app
npx vitest run src/features/dashboards/templateEngine.test.mjs
npm test
```

Expected: all template tests and the complete Vitest suite PASS.

- [ ] **Step 6: Commit the reusable template**

```bash
git add src/app/src/features/dashboards/templates/operationsPerformance.ts src/app/src/features/dashboards/templates/catalog.ts src/app/src/features/dashboards/templateEngine.test.mjs
git commit -m "feat(dashboards): add operations performance template"
```

---

### Task 2: Add a validated, deterministic published Operations sample

**Files:**

- Modify: `src/api/Infrastructure/Persistence/DemoDataSeeder.cs`
- Modify: `src/api.Tests/Program.cs`

**Interfaces:**

- Consumes: `OperationalPerformanceFormId`, `CreateOperationalPerformanceSchema()`, `DashboardDefinitionValidator.Validate()`, `DashboardRevisionSnapshotDefinition`, and existing dashboard JSON serialization.
- Produces: `OperationsPerformanceDashboardId`, `CreateOperationsPerformanceDashboardSeed(Guid creatorId)`, and additive `EnsureOperationsPerformanceDashboardAsync(...)` seeding.

- [ ] **Step 1: Write failing backend seed-contract assertions**

Add assertions after the existing sample-dashboard validator coverage:

```csharp
var operationsDashboard = DemoDataSeeder.CreateOperationsPerformanceDashboardSeed(Guid.Parse("30000000-0000-0000-0000-000000000002"));
var operationsConfig = operationsDashboard.ConfigJson.RootElement.Deserialize<SavedDashboardConfigDefinition>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
var operationsLayout = operationsDashboard.LayoutJson.RootElement.Deserialize<SavedDashboardLayoutDefinition>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
var operationsSnapshot = operationsDashboard.PublishedSnapshotJson!.RootElement.Deserialize<DashboardRevisionSnapshotDefinition>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
var operationsSource = new[] { new DashboardSourceDefinition(DemoDataSeeder.OperationalPerformanceFormId, DemoDataSeeder.CreateOperationalPerformanceSchema(), Array.Empty<DashboardSourceReportDefinition>()) };

AssertEqual(Guid.Parse("11000000-0000-0000-0000-000000000013"), DemoDataSeeder.OperationsPerformanceDashboardId, "The Operations sample dashboard ID should remain deterministic.");
AssertEqual("Operations Performance Sample", operationsDashboard.Name, "The seeded Operations dashboard should use the approved name.");
AssertEqual("operations-performance-sample", operationsDashboard.Slug, "The seeded Operations dashboard should use the approved slug.");
AssertEqual(DashboardPublicationStatuses.Published, operationsDashboard.Status, "The seeded Operations dashboard should be published.");
AssertFalse(operationsDashboard.ShowInNavigation, "The seeded Operations dashboard should remain out of navigation.");
AssertEqual(7, operationsConfig.Sections!.Count, "The seeded Operations dashboard should contain seven sections.");
AssertEqual(24, operationsConfig.Widgets.Count, "The seeded Operations dashboard should contain 24 widgets.");
AssertEqual(5, operationsConfig.Filters!.Count, "The seeded Operations dashboard should contain five filters.");
AssertEqual("operations-performance", operationsConfig.TemplateProvenance!.TemplateId, "The seeded dashboard should retain template provenance.");
AssertEqual(1, operationsConfig.TemplateProvenance.TemplateVersion, "The seeded dashboard should retain template version 1.");
AssertEqual(24, operationsLayout.Widgets.Count, "Every seeded Operations widget should have layout metadata.");
AssertTrue(DashboardDefinitionValidator.Validate(operationsConfig, operationsLayout, operationsSource).Valid, "The seeded Operations dashboard should pass the normal backend validator.");
AssertEqual("operations-performance-sample", operationsSnapshot.Publication.Slug, "The immutable published snapshot should expose the approved slug.");
AssertEqual(DashboardVisibilityModes.Workspace, operationsSnapshot.Settings.Visibility, "The published sample should be workspace-visible.");
AssertFalse(operationsSnapshot.Settings.IsDefault, "The published sample should not become the workspace default.");
```

- [ ] **Step 2: Run the backend harness and confirm the expected failure**

Run:

```bash
dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj
```

Expected: compilation FAIL because `OperationsPerformanceDashboardId` and `CreateOperationsPerformanceDashboardSeed` do not exist.

- [ ] **Step 3: Add the deterministic ID and pure dashboard factory**

Add beside the existing Operations form IDs:

```csharp
public static readonly Guid OperationsPerformanceDashboardId = Guid.Parse("11000000-0000-0000-0000-000000000013");
private static readonly DateTimeOffset OperationsPerformancePublishedAt = new(2026, 8, 25, 0, 0, 0, TimeSpan.Zero);
```

Add `CreateOperationsPerformanceDashboardSeed(Guid creatorId)` as a public deterministic factory so the lightweight harness can validate the exact persisted contract without a running database. Build:

- Seven section IDs `operations-overview`, `operations-loss`, `operations-production`, `operations-engineering`, `operations-supply`, `operations-qaqc`, and `operations-trends-records` in that order.
- The following 17 analytics widgets, using IDs prefixed with `operations-`:

```csharp
var analyticsSpecs = new[]
{
    ("operational-facts", "Operational facts", "operations-overview", DashboardWidgetWidths.Small, ChartWidgetTypes.NumberCard, DashboardAnalyticsMetricTypes.Count, (string?)null, (string?)null, (string?)null, Array.Empty<string>()),
    ("total-actual", "Total actual", "operations-overview", DashboardWidgetWidths.Small, ChartWidgetTypes.NumberCard, DashboardAnalyticsMetricTypes.Sum, "actual_value", null, null, Array.Empty<string>()),
    ("total-target", "Total target", "operations-overview", DashboardWidgetWidths.Small, ChartWidgetTypes.NumberCard, DashboardAnalyticsMetricTypes.Sum, "target_value", null, null, Array.Empty<string>()),
    ("performance-by-module", "Performance by module", "operations-overview", DashboardWidgetWidths.Wide, ChartWidgetTypes.ChoiceBreakdown, DashboardAnalyticsMetricTypes.Average, "actual_value", "module", null, Array.Empty<string>()),
    ("loss-actual", "Total loss actual", "operations-loss", DashboardWidgetWidths.Small, ChartWidgetTypes.NumberCard, DashboardAnalyticsMetricTypes.Sum, "actual_value", null, null, Array.Empty<string>()),
    ("loss-by-metric", "Loss by metric", "operations-loss", DashboardWidgetWidths.Wide, ChartWidgetTypes.ChoiceBreakdown, DashboardAnalyticsMetricTypes.Sum, "actual_value", "metric_key", null, Array.Empty<string>()),
    ("production-by-product", "Production by product", "operations-production", DashboardWidgetWidths.Wide, ChartWidgetTypes.ChoiceBreakdown, DashboardAnalyticsMetricTypes.Sum, "actual_value", "product", null, Array.Empty<string>()),
    ("production-trend", "Production trend", "operations-production", DashboardWidgetWidths.Wide, ChartWidgetTypes.DateTrend, DashboardAnalyticsMetricTypes.Sum, "actual_value", null, "period_date", Array.Empty<string>()),
    ("engineering-by-equipment", "Engineering performance", "operations-engineering", DashboardWidgetWidths.Wide, ChartWidgetTypes.ChoiceBreakdown, DashboardAnalyticsMetricTypes.Average, "actual_value", "equipment", null, Array.Empty<string>()),
    ("engineering-trend", "Utilities and reliability trend", "operations-engineering", DashboardWidgetWidths.Wide, ChartWidgetTypes.DateTrend, DashboardAnalyticsMetricTypes.Average, "actual_value", null, "period_date", Array.Empty<string>()),
    ("supply-by-product", "Inventory by product", "operations-supply", DashboardWidgetWidths.Wide, ChartWidgetTypes.ChoiceBreakdown, DashboardAnalyticsMetricTypes.Sum, "actual_value", "product", null, Array.Empty<string>()),
    ("supply-by-metric", "Supply-chain KPI families", "operations-supply", DashboardWidgetWidths.Wide, ChartWidgetTypes.ChoiceBreakdown, DashboardAnalyticsMetricTypes.Average, "actual_value", "metric_key", null, Array.Empty<string>()),
    ("qaqc-rate", "QA/QC first-time release", "operations-qaqc", DashboardWidgetWidths.Small, ChartWidgetTypes.NumberCard, DashboardAnalyticsMetricTypes.Average, "actual_value", null, null, Array.Empty<string>()),
    ("qaqc-metrics", "Quality metrics", "operations-qaqc", DashboardWidgetWidths.Wide, ChartWidgetTypes.ChoiceBreakdown, DashboardAnalyticsMetricTypes.Average, "actual_value", "metric_key", null, Array.Empty<string>()),
    ("qaqc-detail", "Quality detail", "operations-qaqc", DashboardWidgetWidths.Full, ChartWidgetTypes.Table, DashboardAnalyticsMetricTypes.Count, null, null, null, new[] { "period_label", "metric_key", "product", "actual_value", "target_value", "unit", "status" }),
    ("operations-over-time", "Operational actual over time", "operations-trends-records", DashboardWidgetWidths.Wide, ChartWidgetTypes.DateTrend, DashboardAnalyticsMetricTypes.Sum, "actual_value", null, "period_date", Array.Empty<string>()),
    ("recent-operations", "Operational detail", "operations-trends-records", DashboardWidgetWidths.Full, ChartWidgetTypes.Table, DashboardAnalyticsMetricTypes.Count, null, null, null, new[] { "module", "metric_key", "period_label", "period_number", "product", "equipment", "actual_value", "target_value", "budget_value", "numerator", "denominator", "unit" })
};
```

- The following seven adapter widgets with independent allowlisted settings:

```csharp
var adapterSpecs = new[]
{
    ("overview-target", "Actual versus target", "operations-overview", DashboardWidgetWidths.Wide, "target_attainment", Settings(new() { ["actual"] = 92, ["target"] = 100, ["unit"] = "%", ["tone"] = "warning", ["sourceLabel"] = "Illustrative Operations sample adapter" })),
    ("loss-target", "Loss actual and standard", "operations-loss", DashboardWidgetWidths.Wide, "combo", Settings(new() { ["labels"] = "Jan|Feb|Mar|Apr|May|Jun", ["primary"] = "8|7|9|6|5|6", ["secondary"] = "7|7|7|6|6|6", ["unit"] = "%", ["sourceLabel"] = "Illustrative Operations sample adapter" })),
    ("production-stack", "Product composition", "operations-production", DashboardWidgetWidths.Wide, "stacked_bar", Settings(new() { ["labels"] = "Q1|Q2|Q3|Q4", ["primary"] = "42|48|51|55", ["secondary"] = "31|34|38|41", ["tertiary"] = "18|21|24|27", ["unit"] = "t", ["sourceLabel"] = "Illustrative Operations sample adapter" })),
    ("engineering-target", "Actual versus engineering standard", "operations-engineering", DashboardWidgetWidths.Wide, "target_line", Settings(new() { ["labels"] = "W1|W2|W3|W4|W5|W6", ["primary"] = "72|69|75|71|68|66", ["secondary"] = "70|70|70|70|70|70", ["unit"] = "%", ["sourceLabel"] = "Illustrative Operations sample adapter" })),
    ("supply-attainment", "Service-level attainment", "operations-supply", DashboardWidgetWidths.Medium, "target_attainment", Settings(new() { ["actual"] = 96, ["target"] = 98, ["unit"] = "%", ["tone"] = "warning", ["sourceLabel"] = "Illustrative Operations sample adapter" })),
    ("actual-budget", "Actual and budget comparison", "operations-trends-records", DashboardWidgetWidths.Wide, "combo", Settings(new() { ["labels"] = "Jan|Feb|Mar|Apr|May|Jun", ["primary"] = "31|35|39|42|46|49", ["secondary"] = "30|34|38|43|45|48", ["unit"] = "%", ["sourceLabel"] = "Illustrative Operations sample adapter" })),
    ("detail-popup", "Period detail preview", "operations-trends-records", DashboardWidgetWidths.Wide, "detail_popup", Settings(new() { ["title"] = "Selected period detail", ["period"] = "2026 Q2", ["rows"] = 18, ["groups"] = "Module|Product|Equipment|Metric", ["sourceLabel"] = "Illustrative Operations sample adapter" }))
};
```

Define `Settings(Dictionary<string, object?> values)` as a local identity helper returning `IReadOnlyDictionary<string, object?>` so tuple inference has one stable type.

- Five filters prefixed with `operations-filter-`: Fiscal year and Period have `ApplyToWidgetIds = null`; Product targets `production-by-product`, `production-trend`, `supply-by-product`, `supply-by-metric`, `qaqc-rate`, `qaqc-metrics`, `qaqc-detail`, and `recent-operations`; Equipment targets `engineering-by-equipment`, `engineering-trend`, and `recent-operations`; Module targets `operational-facts`, `total-actual`, `total-target`, `performance-by-module`, `operations-over-time`, and `recent-operations`.
- Provenance `new DashboardTemplateProvenanceDefinition("operations-performance", 1, OperationsPerformancePublishedAt)`.
- Layout order matching widget order exactly.
- Settings `new DashboardSettingsDefinition(DashboardVisibilityModes.Workspace, false)`.
- Publication `new DashboardPublicationSettingsDefinition(DashboardPublicationStatuses.Published, "operations-performance-sample", false, null, "factory", 0, null)`.

Construct a real immutable published snapshot, then serialize it on the entity:

```csharp
var settings = new DashboardSettingsDefinition(DashboardVisibilityModes.Workspace, false);
var publication = new DashboardPublicationSettingsDefinition(
    DashboardPublicationStatuses.Published,
    "operations-performance-sample",
    false,
    null,
    "factory",
    0,
    null);
var snapshot = new DashboardRevisionSnapshotDefinition(
    "Operations Performance Sample",
    "A focused Operations reference dashboard using permission-filtered analytics and clearly labeled illustrative adapters.",
    config,
    layout,
    settings,
    publication);

return new DashboardDefinition
{
    Id = OperationsPerformanceDashboardId,
    Name = snapshot.Name,
    Description = snapshot.Description,
    Status = DashboardPublicationStatuses.Published,
    Slug = publication.Slug,
    ShowInNavigation = false,
    MenuIcon = publication.MenuIcon,
    PublishedAt = OperationsPerformancePublishedAt,
    PublishedById = creatorId,
    CreatedById = creatorId,
    ConfigJson = SerializeToDocument(config),
    LayoutJson = SerializeToDocument(layout),
    ExtraPropertiesJson = DashboardDefinitionAccess.SerializeSettings(settings),
    PublishedSnapshotJson = SerializeToDocument(snapshot),
    PublishedSlug = publication.Slug,
    PublishedShowInNavigation = false,
    PublishedMenuIcon = publication.MenuIcon,
    PublishedMenuOrder = 0
};
```

Do not reuse the current comprehensive sample’s shared generic settings dictionary. Preserve each adapter settings dictionary shown above so its labels, values, unit, and explanatory source label remain specific to that visualization.

- [ ] **Step 4: Add guarded, additive seeding**

Add this call after `EnsureBusinessPerformanceDashboardAsync(...)`:

```csharp
await EnsureOperationsPerformanceDashboardAsync(dbContext, operationsFormVersion, users, cancellationToken);
```

Implement the guard and required schema validation:

```csharp
private static async Task EnsureOperationsPerformanceDashboardAsync(
    OpenBusinessPlatformDbContext dbContext,
    FormVersion operationsVersion,
    IReadOnlyDictionary<string, User> users,
    CancellationToken cancellationToken)
{
    if (await dbContext.Dashboards.AnyAsync(item => item.Id == OperationsPerformanceDashboardId, cancellationToken)) return;

    var schema = operationsVersion.SchemaJson.RootElement.Deserialize<FormSchemaDefinition>(JsonOptions);
    var requiredFields = new[]
    {
        "module", "metric_key", "fiscal_year", "period_type", "period_label", "period_number", "period_date",
        "product", "equipment", "actual_value", "target_value", "budget_value", "numerator", "denominator", "unit", "status"
    };
    if (schema is null || !requiredFields.All(FormReportableFieldMetadata.GetReportableFieldsById(schema).ContainsKey)) return;

    var creatorId = users["builder.demo@company.test"].Id;
    dbContext.Dashboards.Add(CreateOperationsPerformanceDashboardSeed(creatorId));
    dbContext.AuditLogs.AddRange(
        new AuditLogEntry { Id = Guid.NewGuid(), EntityType = "Dashboard", EntityId = OperationsPerformanceDashboardId, Action = "dashboard_created", UserId = creatorId },
        new AuditLogEntry { Id = Guid.NewGuid(), EntityType = "Dashboard", EntityId = OperationsPerformanceDashboardId, Action = "dashboard_published", UserId = creatorId });
}
```

The `AnyAsync` guard must be the first database-dependent action in this method. That preserves edits, unpublished state, revisions, archive state, and recycle-bin state for the fixed ID.

- [ ] **Step 5: Run backend validation**

Run:

```bash
dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj
dotnet build src/api/OpenBusinessPlatform.Api.csproj
```

Expected: harness prints its success result and the API build succeeds. A NuGet vulnerability-service warning is acceptable only when package restore itself succeeds; compilation errors are not acceptable.

- [ ] **Step 6: Commit the published sample seed**

```bash
git add src/api/Infrastructure/Persistence/DemoDataSeeder.cs src/api.Tests/Program.cs
git commit -m "feat(dashboards): seed operations performance sample"
```

---

### Task 3: Document the Operations template and sample

**Files:**

- Create: `tasks/dashboards/003-operations-performance-dashboard.md`
- Modify: `docs/SEED_DATA_PLAN.md`
- Modify: `docs/COMPREHENSIVE_DASHBOARD_FEATURE_MATRIX.md`

**Interfaces:**

- Consumes: the final frontend and backend behavior from Tasks 1 and 2.
- Produces: implementation acceptance record and accurate developer-facing seed/dashboard documentation.

- [ ] **Step 1: Add the task acceptance record**

Create `tasks/dashboards/003-operations-performance-dashboard.md` with this structure and mark boxes only after the corresponding implementation exists:

```markdown
# Operations Performance Dashboard

## Goal

Provide a focused reusable Operations template and a separate published development sample using the existing Operations form and dashboard platform.

## Acceptance

- [x] The gallery exposes the environment-neutral `operations-performance` version-1 template.
- [x] The template contains seven sections, 24 widgets, five targeted filters, and one report-capable Operations source slot.
- [x] Record-backed widgets use the existing analytics engine and illustrative adapters are labeled.
- [x] Development seeding publishes the workspace-visible `operations-performance-sample` dashboard with an immutable snapshot.
- [x] The fixed dashboard ID is additive and never overwrites edits, publication state, or archive state.
- [x] Existing dashboard permissions, backend validation, hidden-field protection, and builder lifecycle remain unchanged.
- [x] Focused frontend/backend tests and documentation are updated.

## Boundaries

No Operations module, cross-form join, static per-widget query filter, database migration, or chart dependency is introduced.
```

- [ ] **Step 2: Update seed and dashboard feature documentation**

In `docs/SEED_DATA_PLAN.md`, add the fixed slug, seven sections, 24 widgets, five filters, template provenance version 1, published snapshot, and non-overwrite behavior under a new `Operations Performance Dashboard Sample` subsection.

In `docs/COMPREHENSIVE_DASHBOARD_FEATURE_MATRIX.md`, add a row:

```markdown
| Focused Operations sample | Reusable template plus separate published sample | Verified | Seven sections, 24 widgets, five targeted filters, one permissioned Operations source, immutable published snapshot, and additive deterministic seeding. |
```

Do not describe adapter values as live calculations.

- [ ] **Step 3: Check documentation and repository diff**

Run:

```bash
git diff --check
git status --short
```

Expected: no whitespace errors; only the three documentation/task files are uncommitted for this task.

- [ ] **Step 4: Commit documentation**

```bash
git add tasks/dashboards/003-operations-performance-dashboard.md docs/SEED_DATA_PLAN.md docs/COMPREHENSIVE_DASHBOARD_FEATURE_MATRIX.md
git commit -m "docs(dashboards): document operations performance sample"
```

---

### Task 4: Run full automated and local database verification

**Files:**

- Verify only; do not modify source unless a failing check identifies a scoped defect.

**Interfaces:**

- Consumes: completed template, seed, tests, and documentation.
- Produces: fresh evidence that builds pass and development seeding creates exactly one valid Operations sample.

- [ ] **Step 1: Run frontend tests, build, and dashboard bundle guard**

Run:

```bash
cd src/app
npm test
npm run build
npm run quality:dashboard
```

Expected: all commands exit 0.

- [ ] **Step 2: Run backend harness and build**

Run from repository root:

```bash
dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj
dotnet build src/api/OpenBusinessPlatform.Api.csproj
```

Expected: both commands exit 0.

- [ ] **Step 3: Apply normal development seeding**

Run:

```bash
docker compose up -d
dotnet run --project src/api/OpenBusinessPlatform.Api.csproj
```

Wait for the API startup log to report that demo seed data is ready, then stop only the foreground API process with Ctrl-C. Do not remove volumes or reset the database.

- [ ] **Step 4: Prove idempotency through a second startup**

Run the API a second time using the same command. Confirm startup succeeds without a duplicate-key error and without a second dashboard at slug `operations-performance-sample`. Stop only the foreground API process with Ctrl-C.

- [ ] **Step 5: Review the final Git state**

Run:

```bash
git status --short
git log --oneline -4
```

Expected: the working tree is clean and the design, template, seed, and documentation commits are visible. If verification required a scoped fix, rerun the failing command and commit the fix separately before continuing.

---

### Task 5: Verify the user interface at desktop and mobile sizes

**Files:**

- Verify only; screenshots may be stored under the existing ignored `src/app/test-results/` directory.

**Interfaces:**

- Consumes: running API, Vite frontend, demo builder account, template gallery, published directory, builder, and viewer.
- Produces: visual and console evidence for the approved browser acceptance criteria.

- [ ] **Step 1: Start API and frontend on available configured ports**

Run the API and `npm run dev` in separate terminals. If Vite reports port 5174 is already in use, reuse the existing healthy frontend or identify the listener with `lsof -i :5174`; do not start duplicate servers blindly.

- [ ] **Step 2: Verify gallery instantiation as Demo Builder**

Use the `webapp-testing` skill. Log in as `builder.demo@company.test` with the documented local demo password. Open the dashboard builder and verify:

- `Operations Performance` appears in the gallery.
- The gallery requests exactly one Operations source binding.
- Binding `Operational Performance Sample Data` shows no capability error.
- Creating the template produces an independent draft with seven tabs, 24 widgets, and five filters.
- Drag/drop and the widget properties drawer still work on the new draft.

- [ ] **Step 3: Verify the separate published sample**

Open the dashboard directory and then `/dashboards/operations-performance-sample`. Verify:

- The sample is listed as published and is not added to primary navigation.
- All seven tabs open through keyboard and pointer interaction.
- Analytics widgets render database-backed results or explicit permission/empty/error states.
- Adapter widgets display the illustrative source label.
- Fiscal year and Period apply across compatible analytics widgets.
- Product, Equipment, and Module affect only the intended widgets.
- Reset clears active filter values.

- [ ] **Step 4: Verify responsive behavior and console health**

Capture one desktop screenshot around 1440 px width and one mobile screenshot around 390 px width. Confirm KPI cards, tabs, filters, wide charts, and tables remain usable without horizontal page overflow. Review browser console output and require no new uncaught errors or React warnings.

- [ ] **Step 5: Record verification outcome**

If all checks pass, no source commit is needed. If a scoped defect is found, return to the applicable earlier task, add a failing automated test when practical, implement the smallest fix, rerun Tasks 4 and 5, and commit the fix with a specific message.
