import assert from "node:assert/strict";
import { test } from "vitest";
import { formatPrintDateTime, getGeneratedAtPrintMetadata, joinPrintMetadata, requestBrowserPrint } from "./printLayout.ts";

test("print layout helpers format metadata and invoke browser print", () => {
  let printCalls = 0;
  requestBrowserPrint(() => {
    printCalls += 1;
  });

  const printedAt = new Date("2026-06-01T14:05:00Z");

  assert.equal(printCalls, 1);
  assert.equal(formatPrintDateTime(printedAt, "en", "UTC"), "Jun 1, 2026, 2:05 PM");
  assert.equal(getGeneratedAtPrintMetadata(printedAt, "en", "UTC"), "Generated Jun 1, 2026, 2:05 PM");
  assert.equal(joinPrintMetadata(["  Records  ", "", null, undefined, "Page 1 of 2"]), "Records | Page 1 of 2");
});
