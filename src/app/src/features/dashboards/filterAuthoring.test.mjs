import assert from "node:assert/strict";
import { test } from "vitest";
import {
  createDashboardFilter,
  getCompatibleDashboardFilterTypes,
  getDashboardFilterDefaults,
  getMissingRequiredDashboardFilters,
  moveDashboardFilter,
  updateDashboardFilterField
} from "./filterAuthoring.ts";

const choiceField = { id: "region", label: "Region", type: "select", source: "form", options: [{ id: "north", label: "North", value: "North" }], filterable: true, sortable: true, searchable: true, supportsAggregation: false, supportsChoiceGrouping: true };
const dateField = { ...choiceField, id: "event_date", label: "Event date", type: "date", options: [] };

test("filter authoring creates stable, unique controls from field metadata", () => {
  const first = createDashboardFilter("form-1", choiceField, []);
  const second = createDashboardFilter("form-1", choiceField, [first]);
  assert.equal(first.id, "filter-region");
  assert.equal(second.id, "filter-region-2");
  assert.deepEqual(first.options, ["North"]);
  assert.deepEqual(getCompatibleDashboardFilterTypes(dateField), ["date_range"]);
  assert.equal(updateDashboardFilterField(first, dateField).type, "date_range");
});

test("filter ordering, defaults, and required completion are deterministic", () => {
  const first = { ...createDashboardFilter("form-1", choiceField, []), required: true, defaultValue: { fieldId: "region", values: ["North"] } };
  const second = createDashboardFilter("form-1", dateField, [first]);
  assert.deepEqual(moveDashboardFilter([first, second], second.id, -1).map((filter) => filter.id), [second.id, first.id]);
  const defaults = getDashboardFilterDefaults([first, second]);
  assert.deepEqual(defaults[first.id]?.values, ["North"]);
  assert.equal(getMissingRequiredDashboardFilters([first], defaults).length, 0);
  assert.equal(getMissingRequiredDashboardFilters([{ ...second, required: true }], {}).length, 1);
});
