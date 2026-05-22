import assert from "node:assert/strict";
import { test } from "vitest";
import {
  createFormDraftSummary,
  filterFormSummaries,
  formStatuses,
  getFormStatusLabel,
  sampleFormSummaries,
  validateCreateFormDraftInput
} from "./drafts.ts";

test("form draft helpers create, validate, filter, and label drafts", () => {
  assert.deepEqual(formStatuses, ["draft", "published", "archived"]);

  const draft = createFormDraftSummary({
    id: "form_test",
    name: "  Safety inspection  ",
    description: "  Used before opening a site  ",
    now: "2026-05-19T12:00:00.000Z"
  });

  assert.deepEqual(draft, {
    id: "form_test",
    name: "Safety inspection",
    description: "Used before opening a site",
    status: "draft",
    fieldCount: 0,
    currentVersionId: null,
    createdAt: "2026-05-19T12:00:00.000Z",
    updatedAt: "2026-05-19T12:00:00.000Z"
  });

  assert.deepEqual(validateCreateFormDraftInput({ name: "   ", description: " keep me " }), {
    valid: false,
    error: "Form name is required.",
    value: { name: "", description: "keep me" }
  });

  assert.equal(filterFormSummaries(sampleFormSummaries, { query: "expense", status: "all" }).length, 1);
  assert.equal(filterFormSummaries(sampleFormSummaries, { query: "", status: "draft" }).every((form) => form.status === "draft"), true);
  assert.equal(filterFormSummaries(sampleFormSummaries, { query: "does-not-exist", status: "all" }).length, 0);
  assert.equal(getFormStatusLabel("published"), "Published");
});
