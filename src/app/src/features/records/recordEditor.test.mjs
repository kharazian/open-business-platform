import assert from "node:assert/strict";
import { test } from "vitest";
import { createRecordEditDraft, createUpdateRecordRequest, getRecordListPath } from "./recordEditor.ts";

test("record editor helpers clone values and build update metadata", () => {
  const record = {
    id: "record-1",
    formId: "form-1",
    formVersionId: "version-1",
    status: "active",
    values: { site_name: "North plant" },
    schema: { schemaVersion: 1, fields: [], layout: { pages: [] } },
    concurrencyStamp: "record-stamp",
    createdAt: "2026-05-19T13:20:00.000Z",
    createdById: null,
    updatedAt: null,
    updatedById: null
  };

  const draftValues = createRecordEditDraft(record);
  draftValues.site_name = "South plant";

  assert.deepEqual(record.values, { site_name: "North plant" });
  assert.deepEqual(draftValues, { site_name: "South plant" });
  assert.deepEqual(createUpdateRecordRequest(record, draftValues), {
    values: { site_name: "South plant" },
    concurrencyStamp: "record-stamp"
  });
  assert.equal(getRecordListPath(record), "/forms/form-1/records");
});
