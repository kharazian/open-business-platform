import assert from "node:assert/strict";
import { test } from "vitest";
import { createDashboardAdapterWidget, isDashboardAdapterWidgetConfigured } from "./adapters.ts";

const registration = {
  id: "example",
  name: "Example",
  visualizations: [{
    id: "summary",
    name: "Summary",
    settings: [
      { key: "period", label: "Period", type: "select", required: true, options: [{ label: "Week", value: "week" }] },
      { key: "report", label: "Report", type: "text", required: true }
    ]
  }],
  render: () => null
};

test("adapter widgets use safe metadata defaults and require configured fields", () => {
  const widget = createDashboardAdapterWidget(registration);
  assert.equal(widget.settings.period, "week");
  assert.equal(isDashboardAdapterWidgetConfigured(registration, widget), false);
  assert.equal(isDashboardAdapterWidgetConfigured(registration, { ...widget, settings: { ...widget.settings, report: "overview" } }), true);
});
