import assert from "node:assert/strict";
import { test } from "vitest";
import { getRecordDetailPrintDescription, getRecordListPrintDescription, requestBrowserPrint } from "./recordPrint.ts";

test("record print helpers request printing and format descriptions", () => {
  let printCalls = 0;
  requestBrowserPrint(() => {
    printCalls += 1;
  });

  assert.equal(printCalls, 1);
  assert.equal(getRecordListPrintDescription(12, 2, 3, "North plant"), "12 total records | Page 2 of 3 | Filter: North plant");
  assert.equal(getRecordListPrintDescription(1, 1, 1, ""), "1 total record | Page 1 of 1");
  assert.equal(getRecordDetailPrintDescription("May 19, 2026, 1:20 PM", "version-1"), "Submitted May 19, 2026, 1:20 PM | Version version-1");
});
