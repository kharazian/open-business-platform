import assert from "node:assert/strict";
import { test } from "vitest";
import { getReportTablePrintDescription } from "./reportPrint.ts";

test("report print helper describes the visible report page", () => {
  assert.equal(
    getReportTablePrintDescription(37, 2, 4, " north "),
    "37 matching rows | Visible page 2 of 4 | Search: north"
  );
  assert.equal(getReportTablePrintDescription(1, 1, 1, ""), "1 matching row | Visible page 1 of 1");
});
