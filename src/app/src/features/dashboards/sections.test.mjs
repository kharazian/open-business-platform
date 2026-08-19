import assert from "node:assert/strict";
import { test } from "vitest";
import { assignWidgetsToDashboardSections, createDashboardSectionId, normalizeDashboardSections } from "./sections.ts";

test("dashboard sections preserve stable order and normalize positions", () => {
  const sections = normalizeDashboardSections([
    { id: "operations", title: "Operations", order: 0 },
    { id: "finance", title: "Finance", order: 0 },
    { id: "people", title: "People", order: 3 }
  ]);

  assert.deepEqual(sections, [
    { id: "operations", title: "Operations", order: 0 },
    { id: "finance", title: "Finance", order: 1 },
    { id: "people", title: "People", order: 2 }
  ]);
});

test("dashboard sections preserve invalid drafts for backend validation", () => {
  assert.deepEqual(normalizeDashboardSections([{ id: "overview", title: "", order: 4 }]), [
    { id: "overview", title: "", order: 0 }
  ]);
});

test("dashboard widgets keep valid sections and fall back for missing sections", () => {
  const sections = normalizeDashboardSections([{ id: "overview", title: "Overview", order: 0 }]);
  const widgets = assignWidgetsToDashboardSections([
    { id: "one", title: "One", sourceFormId: null, sectionId: "overview" },
    { id: "two", title: "Two", sourceFormId: null, sectionId: "removed" },
    { id: "three", title: "Three", sourceFormId: null }
  ], sections);

  assert.deepEqual(widgets.map((widget) => widget.sectionId), ["overview", "overview", "overview"]);
});

test("dashboard section ids are readable and unique", () => {
  const sections = [{ id: "team-plan", title: "Team plan", order: 0 }];
  assert.equal(createDashboardSectionId("Team plan", sections), "team-plan-2");
  assert.equal(createDashboardSectionId("!!!", sections), "section");
});
