import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { test } from "vitest";
import {
  createRecordEditDraft,
  createUpdateRecordRequest,
  getRecordCreatePath,
  getRecordDetailPath,
  getRecordEditPath,
  getRecordListPath,
  isRecordEditMode
} from "./recordEditor.ts";

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
  assert.equal(getRecordDetailPath("record-1"), "/records/record-1");
  assert.equal(getRecordEditPath("record-1"), "/records/record-1?mode=edit");
  assert.equal(getRecordCreatePath("form-1"), "/forms/form-1/submit");
  assert.equal(isRecordEditMode(new URLSearchParams("mode=edit")), true);
  assert.equal(isRecordEditMode(new URLSearchParams("mode=view")), false);
});

test("record detail page can start directly in edit mode from route query", () => {
  const source = readFileSync(new URL("./pages/RecordDetailPage.tsx", import.meta.url), "utf8");

  assert.equal(source.includes("useSearchParams"), true);
  assert.equal(source.includes("isRecordEditMode"), true);
});
