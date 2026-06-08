import assert from "node:assert/strict";
import { test } from "vitest";
import { downloadRecordPrintTemplatePdf, downloadReportPrintTemplatePdf } from "./api.ts";

test("printing API maps server-side PDF download routes", async () => {
  const calls = [];
  const fetcher = async (input, init = {}) => {
    calls.push({ input, init });
    return { ok: true, blob: async () => new Blob(["%PDF-1.4"]) };
  };

  const recordPdf = await downloadRecordPrintTemplatePdf("version 1", "record 1", fetcher);
  const reportPdf = await downloadReportPrintTemplatePdf(
    "version 1",
    "report 1",
    { page: 2, pageSize: 10, search: "Finance team" },
    fetcher
  );

  assert.equal(recordPdf.size, 8);
  assert.equal(reportPdf.size, 8);
  assert.equal(calls[0].input, "/api/print-template-versions/version%201/records/record%201.pdf");
  assert.deepEqual(calls[0].init, { method: "GET", credentials: "include" });
  assert.equal(
    calls[1].input,
    "/api/print-template-versions/version%201/reports/report%201.pdf?page=2&pageSize=10&search=Finance+team"
  );
  assert.deepEqual(calls[1].init, { method: "GET", credentials: "include" });
});
