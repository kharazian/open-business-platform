import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { test } from "vitest";
import { listRelatedRecordPanels, listRelatedRecordRows } from "../forms/api.ts";
import { getRelatedPageCount, getRelatedPanelKey } from "./relatedRecords.ts";

test("related-record API helpers encode discovery and row paging routes", async () => {
  const calls = [];
  const fetcher = async (input, init = {}) => {
    calls.push({ input, init });
    if (input.includes("source%20form/parent%20order")) {
      return {
        ok: true,
        json: async () => ({
          panel: { sourceFormId: "source form", sourceFormName: "Invoices", sourceFieldId: "parent order", sourceFieldLabel: "Parent", columns: [], totalCount: 0 },
          page: 2,
          pageSize: 5,
          items: []
        })
      };
    }
    return { ok: true, json: async () => ({ totalCount: 0, items: [] }) };
  };

  await listRelatedRecordPanels("record one", 2, 7, fetcher);
  const rows = await listRelatedRecordRows("record one", "source form", "parent order", 2, 5, fetcher);

  assert.equal(calls[0].input, "/api/records/record%20one/related?page=2&pageSize=7");
  assert.equal(calls[1].input, "/api/records/record%20one/related/source%20form/parent%20order?page=2&pageSize=5");
  assert.equal(calls.every((call) => call.init.method === "GET" && call.init.credentials === "include"), true);
  assert.equal(rows.page, 2);
});

test("related-record paging helpers use stable panel keys and bounded page counts", () => {
  assert.equal(getRelatedPanelKey({ sourceFormId: "form-1", sourceFieldId: "parent" }), "form-1:parent");
  assert.equal(getRelatedPageCount(0, 10), 1);
  assert.equal(getRelatedPageCount(21, 10), 3);
});

test("record detail mounts read-only related panels outside edit and print output", () => {
  const pageSource = readFileSync(new URL("./pages/RecordDetailPage.tsx", import.meta.url), "utf8");
  const panelSource = readFileSync(new URL("./components/RelatedRecordsWorkspace.tsx", import.meta.url), "utf8");

  assert.equal(pageSource.includes("!editing && !selectedPrintTemplate"), true);
  assert.equal(pageSource.includes("RelatedRecordsWorkspace"), true);
  assert.equal(panelSource.includes('data-print-hide="true"'), true);
  assert.equal(panelSource.includes("listRelatedRecordPanels"), true);
  assert.equal(panelSource.includes("listRelatedRecordRows"), true);
  assert.equal(panelSource.includes("No related records"), true);
  assert.equal(panelSource.includes("No accessible records refer to this record"), true);
  assert.equal(panelSource.includes("Previous page"), true);
  assert.equal(panelSource.includes(">Create<"), false);
  assert.equal(panelSource.includes(">Delete<"), false);
});
